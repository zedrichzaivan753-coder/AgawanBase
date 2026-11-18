using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// How one match ended, from the human player's point of view.
/// </summary>
public enum MatchResult
{
    Win = 0,
    Loss = 1,
    Draw = 2
}

/// <summary>
/// One finished match, as it is remembered on disk. Plain public fields because
/// JsonUtility only serialises public fields.
/// </summary>
[Serializable]
public class MatchEntry
{
    public int matchId;
    public string result;      // "WIN" / "LOSS" / "DRAW"
    public string outcome;     // "FLAG_HOME" / "ELIMINATION" / "TIME_LIMIT" / "DRAW"
    public float seconds;      // match clock when it ended
    public string mapId;       // "classic" / "palengke" / "schoolcampus"
    public int teamSize;       // characters on EACH side
    public string difficulty;  // "EASY" / "NORMAL" / "HARD"
    public string whenUtc;     // ISO-8601 UTC
}

/// <summary>
/// The payload that is signed. Its JSON is what the signature covers, so nothing may be
/// read from the file without going through <see cref="PlayerProfile"/>.
/// </summary>
[Serializable]
public class ProfileState
{
    public int schema = PlayerProfile.SchemaVersion;
    public string playerName = PlayerProfile.DefaultName;
    public int wins;
    public int losses;
    public int draws;
    public int currentStreak;
    public int bestStreak;
    public int lastMatchId;
    public string lastPlayedUtc = "";
    public List<MatchEntry> recent = new List<MatchEntry>();
}

/// <summary>
/// The on-disk envelope: the payload plus a signature over it.
/// </summary>
[Serializable]
public class ProfileFile
{
    public int schema = PlayerProfile.SchemaVersion;
    public string sig = "";
    public ProfileState payload = new ProfileState();
}

/// <summary>
/// The player's local profile: a display name, a win/loss/draw record, the current and best
/// win streak, the last few matches, and a local best-times board of the fastest victories.
///
/// WHY THIS IS A STATIC CLASS AND NOT A SCRIPTABLE OBJECT
/// -----------------------------------------------------
/// Same reason as <see cref="MatchSettings"/>: the whole game is one scene and Restart and
/// Title both reload it, so anything that must survive a reload has to live outside the
/// scene graph. A static costs no asset file and no Inspector wiring.
///
/// WHY A FILE AND NOT PLAYERPREFS
/// ------------------------------
/// MatchSettings (team size, difficulty, tag hints) and MapSettings (map choice) are small
/// preferences, so PlayerPrefs is right for them. A profile is a RECORD: a name plus a
/// growing history. A file is the appropriate store for it, it can be inspected and deleted
/// by the player, and it lets the save carry an integrity signature - which PlayerPrefs
/// values cannot do. Week 13 SA5 documents that split.
///
/// WHERE IT LIVES
/// --------------
/// Application.persistentDataPath/AgawanBase/profile.json
/// Nothing leaves the device. The game makes no network calls and requests no network
/// permission; there is no account and no server to authenticate against.
///
/// WHAT THE SIGNATURE IS AND IS NOT
/// --------------------------------
/// The file is signed with SHA-256 over the payload JSON, salted with a random 16-byte
/// per-install value kept in PlayerPrefs. That makes the save TAMPER-EVIDENT: a hand-edited
/// file no longer matches its signature, so it is rejected and set aside instead of being
/// trusted. It is NOT encryption and NOT proof against a determined local attacker, who has
/// the salt on the same device. There is no server to verify against, so this is the honest
/// ceiling for an offline single-player game - see the PT5 L15 known limit.
/// </summary>
public static class PlayerProfile
{
    public const int SchemaVersion = 1;
    public const string DefaultName = "PLAYER";
    public const int MaxNameLength = 16;
    public const int MaxRecent = 20;
    public const int MaxBoard = 5;

    const string FolderName = "AgawanBase";
    const string FileName = "profile.json";
    const string RejectedFileName = "profile.rejected.json";
    const string SaltKey = "profile.salt";

    static ProfileState state = new ProfileState();
    static bool loaded;
    static string dataDirectory;
    static string lastError = "";

    /// <summary>True once a save file has been read (or found to be missing) this session.</summary>
    public static bool IsLoaded { get { return loaded; } }

    /// <summary>The last problem hit while reading or writing the save. Empty when all is well.</summary>
    public static string LastError { get { return lastError; } }

    /// <summary>Folder the save file is written to.</summary>
    public static string DataDirectory
    {
        get
        {
            if (!string.IsNullOrEmpty(dataDirectory)) return dataDirectory;
            return Path.Combine(Application.persistentDataPath, FolderName);
        }
    }

    /// <summary>Full path of the save file, so a test or a bug report can name it exactly.</summary>
    public static string SavePath { get { return Path.Combine(DataDirectory, FileName); } }

    // ---------------------------------------------------------------- read access

    public static string PlayerName { get { EnsureLoaded(); return state.playerName; } }
    public static int Wins { get { EnsureLoaded(); return state.wins; } }
    public static int Losses { get { EnsureLoaded(); return state.losses; } }
    public static int Draws { get { EnsureLoaded(); return state.draws; } }
    public static int MatchesPlayed { get { EnsureLoaded(); return state.wins + state.losses + state.draws; } }
    public static int CurrentStreak { get { EnsureLoaded(); return state.currentStreak; } }
    public static int BestStreak { get { EnsureLoaded(); return state.bestStreak; } }
    public static string LastPlayedUtc { get { EnsureLoaded(); return state.lastPlayedUtc; } }
    public static int LastMatchId { get { EnsureLoaded(); return state.lastMatchId; } }

    /// <summary>The next id a match should be recorded under. Ids only ever go up.</summary>
    public static int NextMatchId { get { EnsureLoaded(); return state.lastMatchId + 1; } }

    /// <summary>Recent matches, newest first.</summary>
    public static List<MatchEntry> Recent { get { EnsureLoaded(); return state.recent; } }

    /// <summary>
    /// The local best-times board: the fastest victories, quickest first, at most
    /// <see cref="MaxBoard"/> of them. Ties keep the earlier match.
    /// </summary>
    public static List<MatchEntry> BestWins()
    {
        EnsureLoaded();

        List<MatchEntry> wins = new List<MatchEntry>();
        for (int i = 0; i < state.recent.Count; i++)
        {
            if (state.recent[i] != null && state.recent[i].result == "WIN") wins.Add(state.recent[i]);
        }

        wins.Sort(delegate (MatchEntry a, MatchEntry b)
        {
            int byTime = a.seconds.CompareTo(b.seconds);
            if (byTime != 0) return byTime;
            return a.matchId.CompareTo(b.matchId);
        });

        if (wins.Count > MaxBoard) wins.RemoveRange(MaxBoard, wins.Count - MaxBoard);
        return wins;
    }

    // ---------------------------------------------------------------- write access

    /// <summary>
    /// Clears the cached copy only. Every play session starts by reading the file, which is
    /// what makes the save survive closing the app.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetForNewSession()
    {
        // Domain reload is switched off in this project, so a static keeps its value from the
        // previous play session. Clearing it here means Play always starts from the file.
        state = new ProfileState();
        loaded = false;
        dataDirectory = null;
        lastError = "";
    }

    /// <summary>
    /// Reads the save file from disk. Always hits the disk - it is not a cache lookup - so a
    /// caller can prove a value survived a reload by calling it again.
    /// </summary>
    public static void Load()
    {
        loaded = false;
        state = new ProfileState();
        state.recent = new List<MatchEntry>();

        // A load reports only its OWN outcome. Clearing this first matters because the
        // missing-file branch returns before anything sets an error: without this line a first
        // run would inherit the reason a previous load rejected a file, and LastError would say
        // "the file is empty" about a file that does not exist. (Week 16 regression gate found
        // this as an order-dependent EditMode failure; the test was left untouched.)
        lastError = "";

        string path = SavePath;
        string json;

        try
        {
            if (!File.Exists(path))
            {
                loaded = true;
                Debug.Log("[Profile] no save at " + path + " - starting a fresh profile for " + DefaultName + ".");
                return;
            }

            json = File.ReadAllText(path, Encoding.UTF8);
        }
        catch (Exception e)
        {
            lastError = "could not read " + path + ": " + e.Message;
            loaded = true;
            Debug.LogWarning("[Profile] " + lastError + " - starting a fresh profile, the old file is left alone.");
            return;
        }

        if (!TryParse(json, out state, out lastError))
        {
            // Set the file aside rather than overwriting the only copy: if this ever happens to a
            // real player, the rejected file is the evidence of what went wrong.
            Quarantine(path);
            state = new ProfileState();
            state.recent = new List<MatchEntry>();
            loaded = true;
            Debug.LogWarning("[Profile] save rejected (" + lastError + ") - starting fresh; " +
                             "the old file was kept as " + Path.Combine(DataDirectory, RejectedFileName) + ".");
            return;
        }

        Clamp();
        loaded = true;

        Debug.Log("[Profile] loaded " + state.playerName + " from " + path + " - " +
                  state.wins + "W/" + state.losses + "L/" + state.draws + "D, streak " +
                  state.currentStreak + " (best " + state.bestStreak + "), " +
                  state.recent.Count + " recent match(es).");
    }

    /// <summary>
    /// Sets the display name. Returns false when the input could not be used, in which case
    /// the profile keeps its previous name and nothing is written.
    /// </summary>
    public static bool SetName(string raw)
    {
        EnsureLoaded();

        string clean = SanitizeName(raw);
        if (string.IsNullOrEmpty(clean))
        {
            lastError = "a name of only spaces or control characters is not allowed.";
            Debug.LogWarning("[Profile] name refused: " + lastError);
            return false;
        }

        if (clean == state.playerName)
        {
            Debug.Log("[Profile] name unchanged (" + clean + "), nothing to write.");
            return true;
        }

        string previous = state.playerName;
        state.playerName = clean;

        if (!Save())
        {
            state.playerName = previous;   // never claim a name the file does not hold
            return false;
        }

        Debug.Log("[Profile] name '" + previous + "' -> '" + clean + "' saved.");
        return true;
    }

    /// <summary>
    /// Stores one finished match. Refuses an id that has already been recorded, so a button
    /// pressed twice or a callback that fires twice cannot bank the same match twice.
    /// </summary>
    public static bool RecordMatch(int matchId, MatchResult result, string outcome,
                                   float seconds, string mapId, int teamSize, string difficulty)
    {
        EnsureLoaded();

        if (matchId <= state.lastMatchId)
        {
            lastError = "match id " + matchId + " was already recorded (last id " + state.lastMatchId + ").";
            Debug.LogWarning("[Profile] match REFUSED - " + lastError);
            return false;
        }

        MatchEntry entry = new MatchEntry();
        entry.matchId = matchId;
        entry.result = ResultName(result);
        entry.outcome = string.IsNullOrEmpty(outcome) ? "UNKNOWN" : outcome;
        entry.seconds = Mathf.Max(0f, seconds);
        entry.mapId = string.IsNullOrEmpty(mapId) ? "classic" : mapId;
        entry.teamSize = Mathf.Clamp(teamSize, 1, 4);
        entry.difficulty = string.IsNullOrEmpty(difficulty) ? "NORMAL" : difficulty;
        entry.whenUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        state.lastMatchId = matchId;
        state.lastPlayedUtc = entry.whenUtc;

        if (result == MatchResult.Win)
        {
            state.wins++;
            state.currentStreak++;
            if (state.currentStreak > state.bestStreak) state.bestStreak = state.currentStreak;
        }
        else if (result == MatchResult.Loss)
        {
            state.losses++;
            state.currentStreak = 0;
        }
        else
        {
            state.draws++;
            // A draw does not break a streak: it is not a defeat.
        }

        state.recent.Insert(0, entry);
        if (state.recent.Count > MaxRecent) state.recent.RemoveRange(MaxRecent, state.recent.Count - MaxRecent);

        if (!Save())
        {
            // Roll the counting back so memory and disk never disagree.
            state.recent.Remove(entry);
            if (result == MatchResult.Win) { state.wins--; state.currentStreak--; }
            else if (result == MatchResult.Loss) state.losses--;
            else state.draws--;
            Debug.LogWarning("[Profile] match " + matchId + " was NOT saved: " + lastError);
            return false;
        }

        Debug.Log("[Profile] match " + matchId + " recorded: " + entry.result + " (" + entry.outcome +
                  ") in " + entry.seconds.ToString("0.0") + " s on " + entry.mapId + " at " +
                  entry.difficulty + " " + entry.teamSize + "v" + entry.teamSize + " - now " +
                  state.wins + "W/" + state.losses + "L/" + state.draws + "D, streak " + state.currentStreak + ".");
        return true;
    }

    /// <summary>Erases the profile and the file. Used by the panel's two-press RESET.</summary>
    public static bool ResetProfile()
    {
        state = new ProfileState();
        state.recent = new List<MatchEntry>();
        loaded = true;

        string path = SavePath;
        bool removed = true;

        try
        {
            if (File.Exists(path)) File.Delete(path);
            string rejected = Path.Combine(DataDirectory, RejectedFileName);
            if (File.Exists(rejected)) File.Delete(rejected);
        }
        catch (Exception e)
        {
            removed = false;
            lastError = "could not delete " + path + ": " + e.Message;
            Debug.LogWarning("[Profile] reset: " + lastError);
        }

        Debug.Log("[Profile] profile reset - name back to " + DefaultName + ", record cleared" +
                  (removed ? ", file deleted." : " (the file could not be deleted)."));
        return removed;
    }

    /// <summary>Writes the current state. Returns false and sets LastError instead of throwing.</summary>
    public static bool Save()
    {
        string dir = DataDirectory;
        string path = Path.Combine(dir, FileName);
        string tmp = path + ".tmp";

        try
        {
            Directory.CreateDirectory(dir);

            string payloadJson = JsonUtility.ToJson(state);
            ProfileFile file = new ProfileFile();
            file.schema = SchemaVersion;
            file.payload = state;
            file.sig = ComputeSignature(payloadJson);

            string json = JsonUtility.ToJson(file, true);

            // Written to a temp file first, so a crash mid-write cannot leave a half file
            // where a whole one used to be.
            File.WriteAllText(tmp, json, Encoding.UTF8);

            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);

            lastError = "";
            return true;
        }
        catch (Exception e)
        {
            lastError = "could not write " + path + ": " + e.Message;
            Debug.LogWarning("[Profile] save failed - " + lastError);
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            return false;
        }
    }

    // ---------------------------------------------------------------- test seams

    /// <summary>
    /// Points the profile at another folder (null restores the real one). Public because the
    /// Week 13 PT5 harness has to prove the failure cases without touching the player's save.
    /// </summary>
    public static void SetDataDirectory(string dir)
    {
        dataDirectory = string.IsNullOrEmpty(dir) ? null : dir;
        loaded = false;
        Load();
    }

    /// <summary>
    /// Plants a raw save file so the harness can test a corrupt or hand-edited save. When
    /// <paramref name="correctSignature"/> is false the signature is a deliberate mismatch.
    /// </summary>
    public static bool WriteRawForTests(string payloadJson, bool correctSignature)
    {
        string dir = DataDirectory;
        string path = Path.Combine(dir, FileName);

        try
        {
            Directory.CreateDirectory(dir);

            ProfileFile file = new ProfileFile();
            file.schema = SchemaVersion;
            file.payload = JsonUtility.FromJson<ProfileState>(payloadJson);

            // Signed over the CANONICAL re-serialisation, because that is exactly what the
            // reader recomputes. Signing the raw text would make the check depend on spacing.
            string canonical = JsonUtility.ToJson(file.payload);
            file.sig = correctSignature ? ComputeSignature(canonical) : "0000";

            File.WriteAllText(path, JsonUtility.ToJson(file, true), Encoding.UTF8);
            return true;
        }
        catch (Exception e)
        {
            lastError = "could not plant a test save: " + e.Message;
            Debug.LogWarning("[Profile] " + lastError);
            return false;
        }
    }

    /// <summary>Writes arbitrary text as the save file, for the "not JSON at all" case.</summary>
    public static bool WriteGarbageForTests(string text)
    {
        try
        {
            Directory.CreateDirectory(DataDirectory);
            File.WriteAllText(SavePath, text, Encoding.UTF8);
            return true;
        }
        catch (Exception e)
        {
            lastError = "could not plant a garbage save: " + e.Message;
            Debug.LogWarning("[Profile] " + lastError);
            return false;
        }
    }

    // ---------------------------------------------------------------- internals

    static void EnsureLoaded()
    {
        if (!loaded) Load();
    }

    static bool TryParse(string json, out ProfileState parsed, out string error)
    {
        parsed = null;
        error = "";

        if (string.IsNullOrEmpty(json) || json.Trim().Length == 0)
        {
            error = "the file is empty";
            return false;
        }

        ProfileFile file;
        try
        {
            file = JsonUtility.FromJson<ProfileFile>(json);
        }
        catch (Exception e)
        {
            error = "not readable as a profile: " + e.Message;
            return false;
        }

        if (file == null || file.payload == null)
        {
            error = "the file has no profile payload";
            return false;
        }

        if (file.payload.schema > SchemaVersion || file.schema > SchemaVersion)
        {
            error = "the save was written by a newer version (schema " + file.payload.schema + ")";
            return false;
        }

        if (file.payload.recent == null) file.payload.recent = new List<MatchEntry>();

        for (int i = file.payload.recent.Count - 1; i >= 0; i--)
        {
            if (file.payload.recent[i] == null) file.payload.recent.RemoveAt(i);
        }

        if (string.IsNullOrEmpty(file.payload.playerName)) file.payload.playerName = DefaultName;

        string expected = ComputeSignature(JsonUtility.ToJson(file.payload));
        if (!string.Equals(expected, file.sig, StringComparison.Ordinal))
        {
            error = "the signature does not match the contents (the file was edited by hand)";
            return false;
        }

        parsed = file.payload;
        return true;
    }

    static void Clamp()
    {
        // A save is still a file on a phone. If the numbers are impossible, they are clamped
        // rather than carried around - an edited save must never produce a negative record.
        state.wins = Mathf.Max(0, state.wins);
        state.losses = Mathf.Max(0, state.losses);
        state.draws = Mathf.Max(0, state.draws);
        state.currentStreak = Mathf.Max(0, state.currentStreak);
        state.bestStreak = Mathf.Max(state.bestStreak, state.currentStreak);
        state.lastMatchId = Mathf.Max(0, state.lastMatchId);
        state.playerName = SanitizeName(state.playerName);
        if (string.IsNullOrEmpty(state.playerName)) state.playerName = DefaultName;
    }

    /// <summary>Trims, drops control characters, collapses runs of spaces and caps the length.</summary>
    public static string SanitizeName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";

        StringBuilder sb = new StringBuilder(raw.Length);
        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];
            if (char.IsControl(c)) continue;
            sb.Append(c);
        }

        string text = sb.ToString().Trim();
        while (text.Contains("  ")) text = text.Replace("  ", " ");

        if (text.Length > MaxNameLength) text = text.Substring(0, MaxNameLength).Trim();
        return text;
    }

    static string ResultName(MatchResult result)
    {
        if (result == MatchResult.Win) return "WIN";
        if (result == MatchResult.Loss) return "LOSS";
        return "DRAW";
    }

    static void Quarantine(string path)
    {
        try
        {
            string target = Path.Combine(DataDirectory, RejectedFileName);
            if (File.Exists(target)) File.Delete(target);
            File.Move(path, target);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Profile] could not set the rejected save aside: " + e.Message);
        }
    }

    // ---------------------------------------------------------------- signature

    static byte[] Salt()
    {
        string stored = PlayerPrefs.GetString(SaltKey, "");
        if (!string.IsNullOrEmpty(stored))
        {
            try { return Convert.FromBase64String(stored); }
            catch { /* a damaged salt is simply replaced below */ }
        }

        byte[] fresh = new byte[16];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(fresh);
        }

        PlayerPrefs.SetString(SaltKey, Convert.ToBase64String(fresh));
        PlayerPrefs.Save();
        Debug.Log("[Profile] a new per-install signature salt was created.");
        return fresh;
    }

    /// <summary>SHA-256 over the salt followed by the payload JSON, as lowercase hex.</summary>
    public static string ComputeSignature(string payloadJson)
    {
        byte[] salt = Salt();
        byte[] payload = Encoding.UTF8.GetBytes(payloadJson ?? "");

        byte[] buffer = new byte[salt.Length + payload.Length];
        Buffer.BlockCopy(salt, 0, buffer, 0, salt.Length);
        Buffer.BlockCopy(payload, 0, buffer, salt.Length, payload.Length);

        using (SHA256 sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(buffer);
            StringBuilder hex = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++) hex.Append(hash[i].ToString("x2"));
            return hex.ToString();
        }
    }
}
