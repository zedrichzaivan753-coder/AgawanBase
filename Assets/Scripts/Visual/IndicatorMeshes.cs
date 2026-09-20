using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds every indicator mesh once, in code, and hands the same Mesh instance to every
/// character that asks for it. No imported assets, no textures, no sprites - nine triangles
/// worth of geometry each, which is what keeps this inside the "primitives only" rule.
///
/// CONVENTION: every mesh is built in the XY plane with its normal along +Z. One convention
/// then covers both jobs, and orientation becomes the transform's problem:
///   - the ground ring is rotated flat (90 degrees about X) and never spins with the character;
///   - the check / cross / chevron are rotated to face the camera, which never moves.
///
/// The meshes are cached in statics and null-checked on every access, so a domain reload, a
/// scene reload or a fresh Play session all rebuild them without anyone having to remember to.
/// CharacterIndicator never calls new Mesh() itself: one shared instance per shape is what keeps
/// this from becoming eight copies of the same geometry on an Android build.
/// </summary>
public static class IndicatorMeshes
{
    // ---- sizes, in metres. Public so the indicator, the controller and the tests all agree ----
    //
    // The budget these come from: the game renders at 1600x720 worst case with an orthographic
    // size of 14, which is about 25.7 pixels per metre. A 0.24 m ring is therefore ~6 px, and a
    // 0.42 m icon is ~11 px. Nothing thinner than 0.22 m is legible on the smallest target.

    public const float YouOuter = 0.78f;   // the controlled character's ring: deliberately larger
    public const float YouInner = 0.54f;
    public const float TagOuter = 0.60f;
    public const float TagInner = 0.36f;
    public const float IconSize = 0.42f;
    public const float ArrowWidth = 0.50f;
    public const float ArrowHeight = 0.40f;

    const int RingSegments = 24;    // one arc step per 15 degrees: smooth at 6 px
    const int RingDashes = 6;       // a dashed ring reads as "danger" even in greyscale
    const float DashFill = 0.55f;   // 55% drawn, 45% gap

    static Mesh youRing;
    static Mesh tagRing;
    static Mesh tagRingDashed;
    static Mesh checkMesh;
    static Mesh crossMesh;
    static Mesh chevronMesh;

    /// <summary>The larger solid ring that marks the character the player controls.</summary>
    public static Mesh YouRingMesh
    {
        get { if (youRing == null) youRing = BuildRing(YouOuter, YouInner, 0); return youRing; }
    }

    /// <summary>The smaller solid ring: "you can tag this one".</summary>
    public static Mesh TagRingMesh
    {
        get { if (tagRing == null) tagRing = BuildRing(TagOuter, TagInner, 0); return tagRing; }
    }

    /// <summary>The dashed ring: "this one can tag you". Dashed, not just red, so the meaning
    /// survives with colour removed.</summary>
    public static Mesh TagRingDashedMesh
    {
        get { if (tagRingDashed == null) tagRingDashed = BuildRing(TagOuter, TagInner, RingDashes); return tagRingDashed; }
    }

    public static Mesh CheckMesh
    {
        get { if (checkMesh == null) checkMesh = BuildCheck(); return checkMesh; }
    }

    public static Mesh CrossMesh
    {
        get { if (crossMesh == null) crossMesh = BuildCross(); return crossMesh; }
    }

    public static Mesh ChevronMesh
    {
        get { if (chevronMesh == null) chevronMesh = BuildChevron(); return chevronMesh; }
    }

    /// <summary>
    /// A flat annulus. dashes = 0 gives a solid ring; dashes = 6 gives six arcs with gaps, which
    /// is what makes "danger" readable without relying on the colour red.
    /// </summary>
    static Mesh BuildRing(float outer, float inner, int dashes)
    {
        List<Vector3> verts = new List<Vector3>(128);
        List<int> tris = new List<int>(256);

        if (dashes <= 0)
        {
            AddArcBand(verts, tris, 0f, Mathf.PI * 2f, RingSegments, outer, inner);
        }
        else
        {
            float slot = Mathf.PI * 2f / dashes;

            for (int d = 0; d < dashes; d++)
            {
                float start = d * slot;
                AddArcBand(verts, tris, start, start + slot * DashFill, 4, outer, inner);
            }
        }

        return Finish("IndicatorRing", verts, tris);
    }

    /// <summary>One arc of an annulus, from angle a0 to a1, as a triangle strip of quads.</summary>
    static void AddArcBand(List<Vector3> verts, List<int> tris,
                           float a0, float a1, int segments, float outer, float inner)
    {
        int baseIndex = verts.Count;

        for (int i = 0; i <= segments; i++)
        {
            float a = Mathf.Lerp(a0, a1, (float)i / segments);
            float c = Mathf.Cos(a);
            float s = Mathf.Sin(a);

            verts.Add(new Vector3(c * outer, s * outer, 0f));   // outside edge
            verts.Add(new Vector3(c * inner, s * inner, 0f));   // inside edge
        }

        for (int i = 0; i < segments; i++)
        {
            int o0 = baseIndex + i * 2;
            int n0 = o0 + 1;
            int o1 = o0 + 2;
            int n1 = o0 + 3;

            tris.Add(o0); tris.Add(o1); tris.Add(n0);
            tris.Add(o1); tris.Add(n1); tris.Add(n0);
        }
    }

    /// <summary>A tick mark, drawn as two bars: the short down-stroke then the long up-stroke.</summary>
    static Mesh BuildCheck()
    {
        List<Vector3> verts = new List<Vector3>(8);
        List<int> tris = new List<int>(12);

        float h = IconSize * 0.5f;
        float t = IconSize * 0.22f;

        AddBar(verts, tris, new Vector2(-h * 0.95f, 0.10f * h), new Vector2(-h * 0.18f, -h * 0.80f), t);
        AddBar(verts, tris, new Vector2(-h * 0.18f, -h * 0.80f), new Vector2(h * 0.95f, h * 0.80f), t);

        return Finish("IndicatorCheck", verts, tris);
    }

    /// <summary>An X, drawn as two crossing bars.</summary>
    static Mesh BuildCross()
    {
        List<Vector3> verts = new List<Vector3>(8);
        List<int> tris = new List<int>(12);

        float h = IconSize * 0.5f * 0.80f;
        float t = IconSize * 0.22f;

        AddBar(verts, tris, new Vector2(-h, -h), new Vector2(h, h), t);
        AddBar(verts, tris, new Vector2(-h, h), new Vector2(h, -h), t);

        return Finish("IndicatorCross", verts, tris);
    }

    /// <summary>
    /// A downward chevron: two bars meeting at a low apex, which is the shape that reads as
    /// "this one is yours" from above. Pointing down at the character's head, as specified.
    /// </summary>
    static Mesh BuildChevron()
    {
        List<Vector3> verts = new List<Vector3>(8);
        List<int> tris = new List<int>(12);

        float w = ArrowWidth * 0.5f;
        float h = ArrowHeight * 0.5f;
        float t = 0.10f;

        AddBar(verts, tris, new Vector2(-w, h), new Vector2(0f, -h), t);
        AddBar(verts, tris, new Vector2(0f, -h), new Vector2(w, h), t);

        return Finish("IndicatorChevron", verts, tris);
    }

    /// <summary>A thick line segment from a to b, as one quad.</summary>
    static void AddBar(List<Vector3> verts, List<int> tris, Vector2 a, Vector2 b, float thickness)
    {
        Vector2 dir = (b - a).sqrMagnitude > 0.000001f ? (b - a).normalized : Vector2.up;
        Vector2 n = new Vector2(-dir.y, dir.x) * (thickness * 0.5f);

        int i = verts.Count;

        verts.Add(new Vector3(a.x - n.x, a.y - n.y, 0f));   // 0
        verts.Add(new Vector3(a.x + n.x, a.y + n.y, 0f));   // 1
        verts.Add(new Vector3(b.x + n.x, b.y + n.y, 0f));   // 2
        verts.Add(new Vector3(b.x - n.x, b.y - n.y, 0f));   // 3

        tris.Add(i + 0); tris.Add(i + 1); tris.Add(i + 2);
        tris.Add(i + 0); tris.Add(i + 2); tris.Add(i + 3);
    }

    static Mesh Finish(string meshName, List<Vector3> verts, List<int> tris)
    {
        Mesh m = new Mesh();
        m.name = meshName;
        m.SetVertices(verts);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }
}
