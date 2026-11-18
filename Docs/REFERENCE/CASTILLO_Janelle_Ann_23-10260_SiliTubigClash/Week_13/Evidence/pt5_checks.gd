extends Node
## PT5 player-data checks: drives autoloads/leaderboard.gd against a scratch save file.
## Run: copy this file and a one-node scene using it into tools/, then godot --headless --path <project> res://tools/<scene>.tscn
const SCRATCH := "user://pt5_scratch.json"
var passed := 0
var failed := 0

func c(label: String, ok: bool) -> void:
	print(("PASS  " if ok else "FAIL  ") + label)
	if ok: passed += 1
	else: failed += 1

func board(path := SCRATCH) -> Node:
	var b: Node = Leaderboard.get_script().new()
	b.save_path = path
	b._load()   # direct call: _ready() would also connect to SeriesManager
	return b

func write(text: String) -> void:
	var f := FileAccess.open(SCRATCH, FileAccess.WRITE)
	f.store_string(text)
	f.close()

func one(n: String, pts := 10) -> Dictionary:
	return {1: {"name": n, "points": pts, "sili_rounds": 1, "eliminations": 2, "rescues": 1, "survivals": 3, "wipes": 0}}

func _ready() -> void:
	DirAccess.remove_absolute(ProjectSettings.globalize_path(SCRATCH))
	var b := board()
	c("L1 no save file: board starts empty without error", b.is_empty())
	c("L2 valid series is accepted", b.bank_series(101, one("Ana"), 5, [{"name": "Ana"}]))
	var r := board()
	c("L3 record survives a reload from disk", r.record_for("Ana").get("points", -1) == 10)
	c("L4 key is case and space insensitive", r.record_for("  aNA ").get("rounds", -1) == 5)
	c("L5 same series id is refused (no double banking)", not b.bank_series(101, one("Ana"), 5, []))
	c("L6 series id 0 is refused", not b.bank_series(0, one("Ben"), 5, []))
	c("L7 zero rounds is refused", not b.bank_series(102, one("Ben"), 0, []))
	c("L8 empty score table is refused", not b.bank_series(103, {}, 5, []))
	b.bank_series(104, one("   "), 5, [])
	c("L9 blank name creates no record", b.record_for("").is_empty() and b.standings().size() == 1)
	for bad in ["{ not json", "[1,2,3]", "", "{\"records\": \"nope\"}"]:
		write(bad)
		c("L10 unusable save starts empty, no crash: " + JSON.stringify(bad), board().is_empty())
	write("{\"records\":{\"x\":{\"name\":\"X\",\"points\":\"lots\",\"rounds\":5.9},\"y\":7}}")
	var e := board()
	c("L11 hand-edited types are coerced to int", typeof(e.record_for("x")["points"]) == TYPE_INT and e.record_for("x")["rounds"] == 5)
	c("L12 non-dictionary row is skipped", e.record_for("y").is_empty())
	var w := board()
	w.bank_series(105, one("Cy"), 5, [])
	w.reset()
	c("L13 reset empties the board on disk", board().is_empty())
	c("L14 unwritable path does not crash", board("user://no_such_dir/x.json").bank_series(106, one("Di"), 5, []))
	# Documented weakness, asserted so the record shows the real behaviour.
	write("{\"records\":{\"ana\":{\"name\":\"Ana\",\"points\":99999,\"rounds\":5}}}")
	c("L15 KNOWN LIMIT: an edited score is accepted (save is not signed)", board().record_for("Ana")["points"] == 99999)
	DirAccess.remove_absolute(ProjectSettings.globalize_path(SCRATCH))
	print("TOTAL: %d passed, %d failed" % [passed, failed])
	get_tree().quit(1 if failed > 0 else 0)
