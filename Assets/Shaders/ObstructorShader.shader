Shader "Custom/ObstructorShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

        _PeepholeRadius ("Peephole Radius", Range(0, 1)) = 0.25
        _MaxDepth ("Max Depth", Range(0, 255)) = 15
        _MinY ("Min Y", Range(0, 255)) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows alpha:fade

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float eyeDepth;
            float4 screenPos;
            float3 worldPos : TEXCOORD0;
        };

        float distance_from_center_screen(float4 screenPos)
        {
            float3 h = (0.5, 0.5, 0.5);
            float3 pos = (screenPos.xyz / screenPos.w) - h;
            float aspectRatio = _ScreenParams.x / _ScreenParams.y;
            pos.x *= aspectRatio;

            return sqrt((pos.x * pos.x) + (pos.y * pos.y));
        }

        float distance_from_camera(float3 worldPos)
        {
            // Find the distance between worldPos and the plane defined by
            // the camera's forward vector.
            float3 cameraForward = mul((float3x3)unity_CameraToWorld, float3(0,0,1));
            float3 offsetFromCamera = worldPos - _WorldSpaceCameraPos;

            return dot(offsetFromCamera, cameraForward);
        }

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        float _PeepholeRadius;
        float _MaxDepth;
        float _MinY;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            COMPUTE_EYEDEPTH(o.eyeDepth);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;

            // Make pixels in the center of the screen transparent, but only if:
            // * They are above the player's feet in world space
            // * They are closer to the screen than the player is
            float centerDist = distance_from_center_screen(IN.screenPos);
            float cameraDist = distance_from_camera(IN.worldPos);

            if (centerDist < _PeepholeRadius && IN.worldPos.y > _MinY && cameraDist < _MaxDepth)
                c.a = 0;

            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }

        ENDCG
    }
    FallBack "Diffuse"
}
