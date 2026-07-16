using UnityEngine;

/// <summary>
/// Tag component placed on the runtime "SectorRuntime" container so
/// <see cref="SectorRuntimeBootstrap"/> can detect (and avoid duplicating) an
/// existing runtime setup on a scene.
/// </summary>
public class SectorRuntimeMarker : MonoBehaviour { }
