float3 WindDirection = float3(1, 0, 0);
float  WindStrength  = 5.0f;
float  WindFrequency = 1.5f;
float  WindTime      = 0.0f;
float  WindHeightRange = 8.0f;

float3 ObjectWorldPos = float3(0, 0, 0);

float3 ApplyWind(float3 localPos, float3 worldPos)
{
    float heightWeight = saturate(localPos.y / max(WindHeightRange, 0.0001f));

    float phase = dot(ObjectWorldPos.xz, float2(0.3f, 0.7f)) + WindTime * WindFrequency;
    float detailPhase = dot(localPos.xz, float2(0.4f, 0.6f)) * 0.5f;
    
    float sway = sin(phase + detailPhase) * WindStrength * heightWeight;

    return worldPos + WindDirection * sway;
}