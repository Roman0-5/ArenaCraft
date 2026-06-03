using UnityEngine;

namespace ArenaCraft
{
    /// <summary>
    /// Configures the shared-screen camera as an angled orthographic ("2.5D")
    /// view that frames the whole arena so both players are always visible
    /// (GDD 1.5 / FMR10). No split-screen — that is the post-MVP FCR3 option.
    /// </summary>
    public class ArenaCamera : MonoBehaviour
    {
        public void Setup(Camera cam, float arenaRadius)
        {
            cam.orthographic = true;
            cam.orthographicSize = arenaRadius * 1.15f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            cam.backgroundColor = new Color(0.06f, 0.05f, 0.05f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            // Classic isometric-ish tilt looking down at the arena centre.
            float dist = arenaRadius * 2.4f;
            float pitch = 42f;
            Quaternion rot = Quaternion.Euler(pitch, -35f, 0f);
            Vector3 dir = rot * Vector3.forward;
            cam.transform.position = -dir * dist + Vector3.up * 2f;
            cam.transform.rotation = rot;
        }
    }
}
