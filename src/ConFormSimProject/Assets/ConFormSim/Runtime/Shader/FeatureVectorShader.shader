Shader "Hidden/FeatureVectorShader"
{
    CGINCLUDE

    // Max allowed vector length. Default to 256.
    #define _MAX_VECTOR_LENGTH 256
    // The feature vector storing all the values for the currently 
    // rendered object.  
    float _FeatureVector[_MAX_VECTOR_LENGTH];
    // The real length of the feature vector
    int _FeatureVectorLength = 0;
    // The Instance ID of the feature vector definition, corresponding to the
    // feature vector. If _FVDefinitionID and _CurrentFVDefinitionID mismatch,
    // the default vector is rendered.
    int _FVDefinitionID = 0;
    // The default feature vector to read from, if there is no feature
    // vector defined. This applies if _FeatureVectorLength <= 0
    float _DefaultFeatureVector[_MAX_VECTOR_LENGTH];
    // The position in the feature vector to start reading from. 
    int _StartIndex = 0;
    // The instance id of the feature vector definition, which is currently
    // rendered. If _FVDefinitionID and _CurrentFVDefinitionID mismatch, the
    // default vector is rendered.
    int _CurrentFVDefinitionID = 0;

    float4 blockFromVec(float vec[_MAX_VECTOR_LENGTH], int index)
    {
        float4 block;
        // get values foreach index of the block
        for (int i = 0; i < 4; i++)
        {
            // if the block covers areas outside the feature vector, pad all 
            // the outside values with zeros.  
            if (index + i >= _FeatureVectorLength || index + i < 0)
            {
                block[i] = 0;
            }
            // else fill the value from vec
            else 
            {
                block[i] = vec[index + i];
                // block[i] = index + i;
            }
        }

        return block;
    }

    float4 Output()
    {
        // If the feature vector definitions IDs mismatch, render default vector
        if (_FVDefinitionID != _CurrentFVDefinitionID || _FeatureVectorLength <= 0)
        {
            return blockFromVec(_DefaultFeatureVector, _StartIndex);
        }
        // Else if the feature vector is supposed to be bigger than the 
        // _Max_Vector_Length cap it to the max allowed length.
        else if (_FeatureVectorLength > _MAX_VECTOR_LENGTH)
        {
            _FeatureVectorLength = _MAX_VECTOR_LENGTH;
        }
        
        return blockFromVec(_FeatureVector, _StartIndex);
    }
    ENDCG

    SubShader 
    {
        Lighting Off 
        Tags { "RenderType"="Opaque" }
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f 
            {
                float4 pos : SV_POSITION;
                float4 nz : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            v2f vert( appdata_base v ) 
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                
                return o;
            }
            fixed4 frag(v2f i) : SV_Target 
            {
                return Output();
            }
            ENDCG
        }
    }

    SubShader 
    {
        Tags { "RenderType"="TransparentCutout" }
        Pass 
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 nz : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            uniform float4 _MainTex_ST;
            v2f vert( appdata_base v ) 
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                
                return o;
            }
            uniform sampler2D _MainTex;
            uniform fixed _Cutoff;
            uniform fixed4 _Color;
            fixed4 frag(v2f i) : SV_Target 
            {
                fixed4 texcol = tex2D( _MainTex, i.uv );
                clip( texcol.a*_Color.a - _Cutoff );
                return Output();
            }
            ENDCG
        }
    }

    SubShader 
    {
        Tags { "RenderType"="TreeBark" }
        Pass 
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "UnityBuiltin3xTreeLibrary.cginc"
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 nz : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            v2f vert( appdata_full v ) 
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                TreeVertBark(v);
                
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
                
                return o;
            }
            fixed4 frag( v2f i ) : SV_Target 
            {
                return Output();
            }
            ENDCG
        }
    }

    SubShader 
    {
        Tags { "RenderType"="TreeLeaf" }
        Pass 
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "UnityBuiltin3xTreeLibrary.cginc"
            struct v2f 
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 nz : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            v2f vert( appdata_full v ) 
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                TreeVertLeaf(v);
                
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
                
                return o;
            }
            uniform sampler2D _MainTex;
            uniform fixed _Cutoff;
            fixed4 frag( v2f i ) : SV_Target 
            {
                half alpha = tex2D(_MainTex, i.uv).a;

                clip (alpha - _Cutoff);
                return Output();
            }
            ENDCG
        }
    }

    SubShader 
    {
        Tags { "RenderType"="TreeOpaque" "DisableBatching"="True" }
        Pass 
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "TerrainEngine.cginc"
            struct v2f 
            {
                float4 pos : SV_POSITION;
                float4 nz : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            struct appdata 
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            v2f vert( appdata v ) 
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                TerrainAnimateTree(v.vertex, v.color.w);
                o.pos = UnityObjectToClipPos(v.vertex);
                
                return o;
            }
            fixed4 frag(v2f i) : SV_Target 
            {
                return Output();
            }
            ENDCG
        }
    } 

    SubShader 
    {
        Tags { "RenderType"="TreeTransparentCutout" "DisableBatching"="True" }
        Pass 
        {
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "TerrainEngine.cginc"

            struct v2f 
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 nz : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            struct appdata 
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                float4 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            v2f vert( appdata v ) 
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                TerrainAnimateTree(v.vertex, v.color.w);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
                
                return o;
            }
            uniform sampler2D _MainTex;
            uniform fixed _Cutoff;
            fixed4 frag(v2f i) : SV_Target 
            {
                half alpha = tex2D(_MainTex, i.uv).a;

                clip (alpha - _Cutoff);
                return Output();
            }
            ENDCG
        }
        Pass 
        {
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "TerrainEngine.cginc"

            struct v2f 
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 nz : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            struct appdata 
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                float4 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            v2f vert( appdata v ) 
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                TerrainAnimateTree(v.vertex, v.color.w);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
                o.nz.xyz = -COMPUTE_VIEW_NORMAL;
                o.nz.w = COMPUTE_DEPTH_01;
                return o;
            }
            uniform sampler2D _MainTex;
            uniform fixed _Cutoff;
            fixed4 frag(v2f i) : SV_Target 
            {
                fixed4 texcol = tex2D( _MainTex, i.uv );
                clip( texcol.a - _Cutoff );
                return Output();
            }
            ENDCG
        }

    }

    SubShader 
    {
        Tags { "RenderType"="TreeBillboard" }
        Pass 
        {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "TerrainEngine.cginc"
            struct v2f 
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 nz : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            v2f vert (appdata_tree_billboard v) 
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                TerrainBillboardTree(v.vertex, v.texcoord1.xy, v.texcoord.y);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv.x = v.texcoord.x;
                o.uv.y = v.texcoord.y > 0;
                o.nz.xyz = float3(0,0,1);
                o.nz.w = COMPUTE_DEPTH_01;
                return o;
            }
            uniform sampler2D _MainTex;
            fixed4 frag(v2f i) : SV_Target 
            {
                fixed4 texcol = tex2D( _MainTex, i.uv );
                clip( texcol.a - 0.001 );
                return Output();
            }
            ENDCG
        }
    }

    SubShader 
    {
        Tags { "RenderType"="GrassBillboard" }
        Pass 
        {
            Cull Off		
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "TerrainEngine.cginc"

            struct v2f 
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 nz : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata_full v) 
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                WavingGrassBillboardVert (v);
                o.color = v.color;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
                
                return o;
            }
            uniform sampler2D _MainTex;
            uniform fixed _Cutoff;
            fixed4 frag(v2f i) : SV_Target 
            {
                fixed4 texcol = tex2D( _MainTex, i.uv );
                fixed alpha = texcol.a * i.color.a;
                clip( alpha - _Cutoff );
                return Output();
            }
            ENDCG
        }
    }

    SubShader 
    {
        Tags { "RenderType"="Grass" }
        Pass 
        {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "TerrainEngine.cginc"
            struct v2f 
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 nz : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata_full v) 
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                WavingGrassVert (v);
                o.color = v.color;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                
                return o;
            }
            uniform sampler2D _MainTex;
            uniform fixed _Cutoff;
            fixed4 frag(v2f i) : SV_Target 
            {
                fixed4 texcol = tex2D( _MainTex, i.uv );
                fixed alpha = texcol.a * i.color.a;
                clip( alpha - _Cutoff );
                return Output();
            }
            ENDCG
        }
    }
    Fallback Off
}

