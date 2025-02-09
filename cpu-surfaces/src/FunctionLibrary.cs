// C# function implementations for CPU execution w/o compute shaders
using UnityEngine;
using static UnityEngine.Mathf;

public static class FunctionLibrary
{
    // Allows us to reference a method
    // Delegates define the kind of method something can reference
    public delegate Vector3 Function(float u, float v, float t);

    public enum FunctionName
    {
        Wave,
        WaveZ,
        MultiWave,
        MultiWaveZ,
        Ripple,
        RippleZ,
        Sphere,
        SphereTwist,
        Torus
    }

    private static Function[] _functions = {
        Wave,
        WaveZ,
        MultiWave,
        MultiWaveZ,
        Ripple,
        RippleZ,
        Sphere,
        SphereTwist,
        Torus
    };

    // => is an expression body that simplifies the getter statement
    public static int FunctionCount => _functions.Length;

    public static Function GetFunction(FunctionName index) => _functions[(int)index];

    public static Vector3 Wave(float u, float v, float t)
    {
        Vector3 vec3;
        vec3.x = u;
        vec3.y = Sin(PI * (u + t));
        vec3.z = v;
        return vec3;
    }

    // Extended wave function that varies on sin(u + v)
    public static Vector3 WaveZ(float u, float v, float t)
    {
        Vector3 vec3;
        vec3.x = u;
        vec3.y = Sin(PI * (u + v + t));
        vec3.z = v;
        return vec3;
    }

    public static Vector3 MultiWave(float u, float v, float t)
    {
        Vector3 vec3;
        vec3.x = u;
        vec3.y = Sin(PI * (u + 0.5f * t));
        // This is [-1,1] + [-1/2,1/2] = [-3/2,3/2] so mult final result by 2/3 to keep in range [-1,1]
        // var * (const/const) is more efficient than var/const
        vec3.y += Sin(2f * PI * (u + t)) * (1f / 2f);
        vec3.y *= (2f / 3f);
        vec3.z = v;
        return vec3;
    }

    // Extended multiwave function that varies on sin(u) + sin(v)
    public static Vector3 MultiWaveZ(float u, float v, float t)
    {
        Vector3 vec3;
        vec3.x = u;
        vec3.y = Sin(PI * (u + 0.5f * t));
        vec3.y += Sin(2f * PI * (v + t)) * (1f / 2f);
        vec3.y += Sin(PI * (u + v + 0.25f * t));
        vec3.y *= (1f / 2.5f);
        vec3.z = v;
        return vec3;
    }

    public static Vector3 Ripple(float u, float v, float t)
    {
        float d = Abs(u);
        
        Vector3 vec3;
        vec3.x = u;
        vec3.y = Sin(PI * (4f * d - t)) / (1 + 10 * d);
        vec3.z = v;
        return vec3;
    }

    // Extended ripple function that spreads in all directions
    public static Vector3 RippleZ(float u, float v, float t)
    {
        float d = Sqrt(u * u + v * v);
        
        Vector3 vec3;
        vec3.x = u;
        vec3.y = Sin(PI * (4f * d - t)) / (1 + 10 * d);
        vec3.z = v;
        return vec3;
    }

    public static Vector3 Sphere(float u, float v, float t)
    {
        // Uniform radius
        float r = 1 + 0.5f * Sin(PI * t);
        float s = r * Cos(0.5f * PI * v);

        Vector3 vec3;
        vec3.x = s * Sin(PI * u);
        vec3.y = r * Sin(0.5f * PI * v);
        vec3.z = s * Cos(PI * u);
        return vec3;
    }

    public static Vector3 SphereTwist(float u, float v, float t)
    {
        float r;
        // Weird non uniform vertical bands
        // r = 0.1f * (9 + Sin (8 * PI *  u * t)); 
        // Weird non uniform horizontal bands
        // r = 0.1f * (9 + Sin (8 * PI *  v * t)); 
        // Twisted bands
        r = 0.1f * (9 + Sin(PI * (12.0f * u + 8.0f * v + t)));

        float s = r * Cos(0.5f * PI * v);

        Vector3 vec3;
        vec3.x = s * Sin(PI * u);
        vec3.y = r * Sin(0.5f * PI * v);
        vec3.z = s * Cos(PI * u);
        return vec3;
    }

    public static Vector3 Torus(float u, float v, float t)
    {
        // Major radus, size of hole
        float r1 = .7f + .1f * Sin(PI * (8.0f * u + 0.5f * t));
        // Minor radius, thickness of ring
        float r2 = 3.0f / 20.0f + (1.0f / 20.0f) * Sin(PI * (16.0f * u + 8.0f * v + 3.0f * t));
        float s = r1 + r2 * Cos(PI * v);
        
        Vector3 vec3;
        vec3.x = s * Sin(PI * u);
        vec3.y = r2 * Sin(PI * v);
        vec3.z = s * Cos(PI * u);
        return vec3;
    }

    public static FunctionName GetNextFunctionName(FunctionName name)
    {
        if ((int)name >= _functions.Length - 1)
            return 0;
        return name + 1;
    }

    public static Vector3 Morph(float u, float v, float t, Function from, Function to, float progress)
    {
        // SmoothStep is 3x^2 - 2x^3 and is used to ease in and out
        // The morph transition will begin and end slower
        Vector3 vec3 = Vector3.LerpUnclamped(from(u, v, t), to(u, v, t), SmoothStep(0f, 1f, progress));
        return vec3;
    }
}