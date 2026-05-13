#ifndef FOG_INCLUDE
#define FOG_INCLUDE

// 颜色渐变参数
float3 _FogColorStart;      // 渐变起始颜色
float3 _FogColorEnd;        // 渐变结束颜色
float _FogColorBlend;       // 颜色混合程度
//float _FogGradientDirection; // 0=垂直,1=水平,2=对角线

float _FogGlobalDensity;
float _FogFallOff;
float _FogHeightStart;
float _FogHeightEnd;
float _FogStartDis;
//float _FogInscatteringExp;
float _FogGradientDis;

// 全局雾参数
float _GlobalFogDensity;
float _GlobalFogStartDis;
float _GlobalFogEndDis;

half3 ExponentialHeightFog(half3 col, half3 positionWS, float4 screenPos)
{
    // 计算高度雾因子
    half heightFactor = saturate((positionWS.y - _FogHeightStart) / (_FogHeightEnd - _FogHeightStart));
    half heightFallOff = _FogFallOff * 0.01;
    half falloff = heightFallOff * (positionWS.y - _WorldSpaceCameraPos.y - _FogHeightStart);
    half fogDensity = _FogGlobalDensity * exp2(-falloff);
    half fogFactor = (1 - exp2(-falloff)) / falloff;
    
    // 计算距离因子
    half3 viewDir = _WorldSpaceCameraPos - positionWS;
    half rayLength = length(viewDir);
    half distanceFactor = max((rayLength - _FogStartDis) / _FogGradientDis, 0);
    
    // 平面雾计算
    half planarFog = fogFactor * fogDensity * distanceFactor * (1 - heightFactor);

    // 全局距离雾计算
    half globalFog = saturate((rayLength - _GlobalFogStartDis) / (_GlobalFogEndDis - _GlobalFogStartDis));
    globalFog *= _GlobalFogDensity;

    // 合并雾效
    half totalFog = max(planarFog, globalFog);

    // 计算屏幕UV渐变
    float2 screenUV = screenPos.xy / screenPos.w;
    screenUV = screenUV * 0.5 + 0.5; 

    float gradientFactor = screenUV.y;
    
    half fogColorFactor = smoothstep(_FogColorBlend, 1, saturate(gradientFactor));
    
    // 混合两种雾颜色
    half3 fogColor = lerp(_FogColorStart, _FogColorEnd, fogColorFactor);

    // 光照散射计算
    //Light mainLight = GetMainLight();
    //half3 lightDir = mainLight.direction;
    //half inscatterFactor = pow(saturate(dot(-normalize(viewDir), lightDir)), _FogInscatteringExp);
    //inscatterFactor *= 1 - saturate(exp2(falloff));
    //inscatterFactor *= distanceFactor;
    //half3 finalFogColor = lerp(fogColor, mainLight.color, saturate(inscatterFactor));
    //return lerp(col, finalFogColor, saturate(totalFog));

    return lerp(col, fogColor, saturate(totalFog));
}

half ExponentialHeightFogAlpha(half col, half3 positionWS, float4 screenPos)
{
    // 计算高度雾因子
    half heightFactor = saturate((positionWS.y - _FogHeightStart) / (_FogHeightEnd - _FogHeightStart));
    half heightFallOff = _FogFallOff * 0.01;
    half falloff = heightFallOff * (positionWS.y - _WorldSpaceCameraPos.y - _FogHeightStart);
    half fogDensity = _FogGlobalDensity * exp2(-falloff);
    half fogFactor = (1 - exp2(-falloff)) / falloff;
    
    // 计算距离因子
    half3 viewDir = _WorldSpaceCameraPos - positionWS;
    half rayLength = length(viewDir);
    half distanceFactor = max((rayLength - _FogStartDis) / _FogGradientDis, 0);
    
    // 平面雾计算
    half planarFog = fogFactor * fogDensity * distanceFactor * (1 - heightFactor);

    // 全局距离雾计算
    half globalFog = saturate((rayLength - _GlobalFogStartDis) / (_GlobalFogEndDis - _GlobalFogStartDis));
    globalFog *= _GlobalFogDensity;

    // 合并雾效
    half totalFog = max(planarFog, globalFog);

    // 计算屏幕UV渐变
    float2 screenUV = screenPos.xy / screenPos.w;
    screenUV = screenUV * 0.5 + 0.5; 

    float gradientFactor = screenUV.y;
    
    half fogColorFactor = smoothstep(_FogColorBlend, 1, saturate(gradientFactor));
    
    // 混合两种雾颜色
    half fogColor = lerp(col, 0, fogColorFactor);

    // 光照散射计算
    //Light mainLight = GetMainLight();
    //half3 lightDir = mainLight.direction;
    //half inscatterFactor = pow(saturate(dot(-normalize(viewDir), lightDir)), _FogInscatteringExp);
    //inscatterFactor *= 1 - saturate(exp2(falloff));
    //inscatterFactor *= distanceFactor;
    //half3 finalFogColor = lerp(fogColor, mainLight.color, saturate(inscatterFactor));
    //return lerp(col, finalFogColor, saturate(totalFog));

    return lerp(col, fogColor, saturate(totalFog));

    
}

#endif