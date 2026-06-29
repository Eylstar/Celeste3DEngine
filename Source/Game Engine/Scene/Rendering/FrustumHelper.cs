using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

internal class FrustumHelper
{
    internal static Vector3[] GetFrsutumCorners(Camera3D cam, float near, float far)
    {
        Vector3[] corners = new Vector3[8];

        Transform camTransform = cam.transform;
        float fov = cam.FOV;
        float aspect = cam.AspectRatio;
        
        float nearHeight = 2f * (float)Math.Tan(fov * 0.5f) * near;
        float nearWidth  = nearHeight * aspect;

        float farHeight = 2f * (float)Math.Tan(fov * 0.5f) * far;
        float farWidth  = farHeight * aspect;

        Vector3 nearCenter = camTransform.position + camTransform.Forward * near;
        Vector3 farCenter  = camTransform.position + camTransform.Forward * far;

        Vector3 nearUp = camTransform.Up * (nearHeight * 0.5f);
        Vector3 nearRight = camTransform.Right * (nearWidth * 0.5f);

        Vector3 farUp = camTransform.Up * (farHeight * 0.5f);
        Vector3 farRight = camTransform.Right * (farWidth * 0.5f);

        // near
        corners[0] = nearCenter - nearRight + nearUp;
        corners[1] = nearCenter + nearRight + nearUp;
        corners[2] = nearCenter + nearRight - nearUp;
        corners[3] = nearCenter - nearRight - nearUp;

        // far
        corners[4] = farCenter - farRight + farUp;
        corners[5] = farCenter + farRight + farUp;
        corners[6] = farCenter + farRight - farUp;
        corners[7] = farCenter - farRight - farUp;
        
        return corners;
    }
}