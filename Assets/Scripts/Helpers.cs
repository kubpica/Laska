using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable] public class BoolEvent : UnityEvent<bool> { }
[System.Serializable] public class IntEvent : UnityEvent<int> { }
[System.Serializable] public class FloatEvent : UnityEvent<float> { }
[System.Serializable] public class LongEvent : UnityEvent<long> { }
[System.Serializable] public class StringEvent : UnityEvent<string> { }
[System.Serializable] public class CharEvent : UnityEvent<char> { }
[System.Serializable] public class GameObjectEvent : UnityEvent<GameObject> { }

/// <summary>
/// An auxiliary class containing auxiliary methods.
/// </summary>
public static class Helpers
{

    /// <summary>
    /// GUIStyle that has a solid white hover/onHover background to indicate highlighted items. (Get-only)
    /// </summary>
    /// <value> <see href="https://docs.unity3d.com/ScriptReference/GUIStyle.html">Unity's GUIStyle</see>.</value>
    public static GUIStyle ListStyle { get; private set; }

    /// <summary>
    /// Transparent 4x4 sprite. (Get only)
    /// </summary>
    /// <value> <see href="https://docs.unity3d.com/ScriptReference/Sprite.html">Unity's Sprite</see>.</value>
    public static Sprite TransparentSprite { get; private set; }

    static Helpers()
    {
        // Make the ListStyle
        ListStyle = new GUIStyle();
        ListStyle.normal.textColor = Color.white;
        Texture2D tex = new Texture2D(2, 2);
        Color[] colors = new Color[4];
        for (int i = 0; i < 4; i++)
            colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.Apply();
        ListStyle.hover.background = tex;
        ListStyle.onHover.background = tex;
        for (int i = 0; i < 4; i++)
            colors[i] = new Color(0.173f, 0.271f, 0.424f);
        Texture2D tex2 = new Texture2D(2, 2);
        tex2.SetPixels(colors);
        tex2.Apply();
        ListStyle.normal.background = tex2;
        ListStyle.padding.left = ListStyle.padding.right = ListStyle.padding.top = ListStyle.padding.bottom = 4;

        // Make the TransparentSprite
        Texture2D transparent = new Texture2D(4, 4, TextureFormat.ARGB32, false);
        TransparentSprite = Sprite.Create(transparent, new Rect(0, 0, transparent.width, transparent.height), new Vector2(0.5f, 0.5f), transparent.width / 0.4f);
        Color fillColor = Color.clear;
        Color[] fillPixels = new Color[transparent.width * transparent.height];
        for (int i = 0; i < fillPixels.Length; i++)
            fillPixels[i] = fillColor;
        transparent.SetPixels(fillPixels);
        transparent.Apply();
    }

    public static Vector3 GetMouseWorldPoint(this Camera cam)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return hit.point;
        }

        return cam.transform.position + ray.direction * 2;
    }

    public static bool TryGetMouseWorldPoint(this Camera cam, out Vector3 point)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            point = hit.point;
            return true;
        }

        point = cam.transform.position + ray.direction * 2;
        return false;
    }

    public static Collider GetColliderUnderMouse(this Camera cam)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.collider;
        }

        return null;
    }

    public static float FlatDistance(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }

    /// <summary>
    /// Check if a point is on the right or left of a vector. 
    /// </summary>
    /// <seealso cref="https://answers.unity.com/questions/1302113/check-if-a-point-is-on-the-right-or-left-of-a-vect.html"/>
    /// <param name="basePoint"> The point from which "we look".</param>
    /// <param name="baseDirection"> The direction "we look".</param>
    /// <param name="targetPoint"> The point we want to check if it is on "our" left or right side.</param>
    /// <returns> -1 = Left; 0 = Directly in front or behind; 1 = Right</returns>
    public static int SideRelativeTo(Vector3 basePoint, Vector3 baseDirection, Vector3 targetPoint)
    {
        Vector3 targetDirection = targetPoint - basePoint;
        Vector3 cross = Vector3.Cross(targetDirection, baseDirection);

        if (cross.y > 0)
            return 1; // Right
        else if (cross.y < 1)
            return -1; // Left
        else
            return 0; // On the same line as indicated by the base
    }

    public static float FlatAngle(this Camera cam, Vector3 target)
    {
        Vector3 targetDir = target - cam.transform.position;
        targetDir.y = 0;
        Vector3 camDir = cam.transform.forward;
        camDir.y = 0;
        float angle = Vector3.Angle(targetDir, camDir);

        return angle;
    }

    public static float FlatAngle(Vector3 from, Vector3 to)
    {
        from.y = 0;
        to.y = 0;
        return Vector3.Angle(from, to);
    }

    public static float Angle(this Camera cam, Vector3 target)
    {
        Vector3 targetDir = target - cam.transform.position;
        float angle = Vector3.Angle(targetDir, cam.transform.forward);

        return angle;
    }

    public static float Angle(this Camera cam, Transform target)
    {
        float angle = cam.Angle(target.position);
        //Debug.Log(target.gameObject.name + " angle " + angle);

        return angle;
    }

    public static Vector3 Perp(this Vector3 v3)
    {
        return Quaternion.Euler(0, 90, 0) * v3;
    }

    public static IEnumerable<Transform> GetChildrenSortedByNearest(GameObject holder, Vector3 point)
    {
        var children = new List<Transform>();
        for (int i = 0; i < holder.transform.childCount; i++)
        {
            var c = holder.transform.GetChild(i);
            children.Add(c);
        }

        return SortByNearest(children, point);
    }

    public static IEnumerable<Transform> SortByNearest(IEnumerable<Transform> target, Vector3 point)
    {
        return target.OrderBy(t => Vector3.Distance(point, t.position));
    }

    /// <summary>
    /// Convert string to integer.
    /// </summary>
    /// <param name="s"> String with the representation of an integral number.</param>
    /// <returns> On success, the function returns the converted integral number as an int value. If string can not be parsed, returns 0</returns>
    public static int stoi(string s)
    {
        int i;
        if (!int.TryParse(s, out i))
        {
            Debug.LogError("String " + s + " could not be parsed, returning 0");
            return 0;
        }
        return i;
    }

    /// <summary>
    /// Convert string to ulong.
    /// </summary>
    /// <param name="s"> String with the representation of an integral number (ulong).</param>
    /// <returns> On success, the function returns the converted integral number as an ulong value. If string can not be parsed, returns 0</returns>
    public static ulong stoul(string s)
    {
        ulong i;
        if (!ulong.TryParse(s, out i))
        {
            Debug.LogError("String " + s + " could not be parsed, returning 0");
            return 0;
        }
        return i;
    }

    /// <summary>
    /// Convert string to float.
    /// </summary>
    /// <param name="s"> String with the representation of a floating-point number.</param>
    /// <returns> On success, the function returns the converted floating-point number as a value of type float. If string can not be parsed, returns 0</returns>
    public static float stof(string s)
    {
        float f;
        if (!float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out f))
        {
            Debug.LogError("String (" + s + ") could not be parsed, returning 0");
            return 0;
        }
        return f;
    }

    public static bool TryStof(string s, out float f)
    {
        return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out f);
    }

    public static float RoundTo2DecimalPlaces(float f)
    {
        return (float)Math.Round(f * 100f) / 100f;
    }

    /// <summary>
    /// Repaints the entire texture to a given color.
    /// </summary>
    /// <param name="texture"> Texture to repaint.</param>
    /// <param name="fillColor"> New texture color.</param>
    public static void TextureSetColor(Texture2D texture, Color fillColor)
    {
        Color[] fillPixels = new Color[texture.width * texture.height];
        for (int i = 0; i < fillPixels.Length; i++)
            fillPixels[i] = fillColor;
        texture.SetPixels(fillPixels);
        texture.Apply();
    }

    /// <summary>
    /// Repaints the center of the texture to a specific color and the stroke to another.
    /// </summary>
    /// <param name="texture"> Texture to repaint.</param>
    /// <param name="fillColor"> Texture center color.</param>
    /// <param name="outlineColor"> Texture contour color.</param>
    /// <param name="outlinePercentage"> Percentage of texture occupied by texture contour. (1=100%)</param>
    public static void TextureSetColor(Texture2D texture, Color fillColor, Color outlineColor, float outlinePercentage = 0.0f)
    {
        Color[] fillPixels = new Color[texture.width * texture.height];

        if (outlinePercentage == 0.0f || outlineColor.Equals(Color.clear))
        {
            for (int i = 0; i < fillPixels.Length; i++)
                fillPixels[i] = fillColor;
        }
        else
        {
            int outlineSize = (int)(Mathf.Min(texture.width, texture.height) / 2.0f * outlinePercentage);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    if (x <= outlineSize || texture.width - x - 1 <= outlineSize || y <= outlineSize || texture.height - y - 1 <= outlineSize)
                        fillPixels[y * texture.width + x] = outlineColor;
                    else
                        fillPixels[y * texture.width + x] = fillColor;
                }
            }
        }

        texture.SetPixels(fillPixels);
        texture.Apply();
    }

    /// <summary>
    /// Set the global scale of the object.
    /// </summary>
    /// <param name="transform"> <see href="https://docs.unity3d.com/ScriptReference/Transform.html">Unity's Transform</see> of the object to rescale.</param>
    /// <param name="globalScale"> New scale of the object.</param>
    public static void SetGlobalScale(this Transform transform, Vector3 globalScale)
    {
        transform.localScale = Vector3.one;
        transform.localScale = new Vector3(globalScale.x / transform.lossyScale.x, globalScale.y / transform.lossyScale.y, globalScale.z / transform.lossyScale.z);
    }

    public static string HashToSHA256(string data)
    {
        var utf8 = new UTF8Encoding();
        return HashToSHA256(utf8.GetBytes(data));
    }

    public static string HashToSHA256(byte[] data)
    {
        using (SHA256 mySHA256 = SHA256.Create())
        {
            var hash = mySHA256.ComputeHash(data);
            return BitConverter.ToString(hash).Replace("-", String.Empty).ToLower();
        }
    }

    /// <summary>
    /// Allowed: abcdefghijklmnopqrstuvwxyz
    /// ABCDEFGHIJKLMNOPQRSTUVWXYZ
    /// 0123456789
    /// -._@+
    /// Other chars will me removed from the string.
    /// </summary>
    /// <param name="str"> Source.</param>
    /// <returns> String with removed characters.</returns>
    public static string RemoveSpecialCharacters(this string str)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in str)
        {
            if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')
                || c == '.' || c == '_' || c == '-' || c == '@' || c == '+')
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Get Texture2D from .png file.
    /// </summary>
    /// <param name="filePath"> The file.</param>
    /// <returns> The texture.</returns>
    public static Texture2D LoadPNG(string filePath)
    {
        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2, TextureFormat.BGRA32, true);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            tex.filterMode = FilterMode.Trilinear;
        }
        return tex;
    }

    /// <summary>
    /// Transform unity world point to block coordinates (+ alignment to the grid).
    /// </summary>
    /// <param name="v2"> Unity's world point.</param>
    /// <returns> The same point but in block coordinates (rounded to int - grid alignment)./</returns>
    public static Vector2 ToBlockCoords(Vector2 v2)
    {
        //unity's world point to block coordinates: (alignment to the grid)
        if (v2.y > 0)
            v2.y = (int)((v2.y + 0.2f) / 0.4f);
        else
            v2.y = (int)((v2.y - 0.2f) / 0.4f);
        if (v2.x > 0)
            v2.x = (int)((v2.x + 0.2f) / 0.4f);
        else
            v2.x = (int)((v2.x - 0.2f) / 0.4f);

        return v2;
    }

    /// <summary>
    /// Align world point to the block grid.
    /// </summary>
    /// <param name="v2"> World point.</param>
    /// <returns> Matched world point.</returns>
    public static Vector2 GridAlignment(Vector2 v2)
    {
        if (v2.y > 0)
            v2.y = (int)((v2.y + 0.2f) / 0.4f) * 0.4f;
        else
            v2.y = (int)((v2.y - 0.2f) / 0.4f) * 0.4f;
        if (v2.x > 0)
            v2.x = (int)((v2.x + 0.2f) / 0.4f) * 0.4f;
        else
            v2.x = (int)((v2.x - 0.2f) / 0.4f) * 0.4f;

        return v2;
    }

    /// <summary>
    /// Unity's world point pointed by mouse.
    /// </summary>
    /// <returns> World point.</returns>
    public static Vector3 GetMouseWorldPoint()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = 6f;
        return Camera.main.ScreenToWorldPoint(mousePosition);
    }
}