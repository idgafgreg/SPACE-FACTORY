#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// P17 — personal effects (sticky notes, name tags, photos, rations, board games,
/// cartridges) baked onto the desks, crate lids and poster walls near the hub and
/// workshop.
///
/// The bake body lives in <see cref="SceneDressingBake"/>, shared with the other
/// set-piece passes; see that file for why a hand-authored sector needs one at all.
///
/// Tools → Space Factory → Bake Personal Effects Into Scene
/// </summary>
public static class PersonalEffectsBake
{
    const string BakeMenu = "Tools/Space Factory/Bake Personal Effects Into Scene";

    [MenuItem(BakeMenu)]
    public static void BakeMenuItem() => Bake(showDialogs: true);

    [MenuItem(BakeMenu, validate = true)]
    static bool BakeValidate() => !EditorApplication.isPlaying;

    public static int Bake(bool showDialogs = true) =>
        SceneDressingBake.Run<SyntyPersonalEffects>(
            "PersonalEffectsRoot",
            "Bake Personal Effects Into Scene",
            "Dress the desks, crate lids and poster walls near the hub and workshop with the " +
            "pack's personal-effects props: sticky notes, name tags, photos, rations, board " +
            "games and cartridges.",
            showDialogs);
}
#endif
