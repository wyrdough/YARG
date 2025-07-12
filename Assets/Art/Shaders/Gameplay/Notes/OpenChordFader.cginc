#ifndef OPENCHORDFADE_CGINC
#define OPENCHORDFADE_CGINC

void Fade_float(float2 uv, float3x3 RegionBounds, float FadedAlpha, float FadeDistance, out float Alpha)
{
    // All v within the bounds of element 0 and 1 of each row of _RegionBounds must be set to _FadedAlpha
    // Lerp from _FadedAlpha to 1 within _FadeDistance below element 0 and _FadeDistance above element 1

    Alpha = 1.0;

    // First pass: Apply all inner region fades (fully faded areas)
    for (int rowIdx = 0; rowIdx < 3; rowIdx++)
    {
        float2 row = RegionBounds[rowIdx];
        float lower = row.x;
        float upper = row.y;

        // Skip rows with invalid bounds (both values near zero) using a multiplicative mask
        float isValidRow = step(0.00001, max(abs(lower), abs(upper)));

        // Inside region calculation - fully faded region
        float insideRegion = step(lower, uv.y) * step(uv.y, upper) * isValidRow;

        // Apply the inner region fade
        Alpha = lerp(Alpha, FadedAlpha, insideRegion);
    }

    // Second pass: Apply all transition fades (partially faded areas)
    float transitionAlpha = 1.0;

    for (int rowIdx = 0; rowIdx < 3; rowIdx++)
    {
        float2 row = RegionBounds[rowIdx];
        float lower = row.x;
        float upper = row.y;

        // Validity check as a multiplicative mask
        float isValidRow = step(0.00001, max(abs(lower), abs(upper)));

        // Lower fade region calculation
        float lowerLerp = lower - FadeDistance;
        float inLowerFade = step(lowerLerp, uv.y) * step(uv.y, lower) * isValidRow;
        float normalizedLowerPos = saturate((uv.y - lowerLerp) / max(FadeDistance, 0.00001));
        float lowerFadeAlpha = lerp(1.0, FadedAlpha, normalizedLowerPos);

        // Upper fade region calculation
        float upperLerp = upper + FadeDistance;
        float inUpperFade = step(upper, uv.y) * step(uv.y, upperLerp) * isValidRow;
        float normalizedUpperPos = saturate((uv.y - upper) / max(FadeDistance, 0.00001));
        float upperFadeAlpha = lerp(FadedAlpha, 1.0, normalizedUpperPos);

        // Apply the fade effects - we use the minimum alpha to ensure the darkest fade wins
        transitionAlpha = min(transitionAlpha, lerp(1.0, lowerFadeAlpha, inLowerFade));
        transitionAlpha = min(transitionAlpha, lerp(1.0, upperFadeAlpha, inUpperFade));
    }

    // Final alpha is the minimum between the inner region alpha and transition alpha
    Alpha = min(Alpha, transitionAlpha);


    // for (int rowIdx = 0; rowIdx < 3; rowIdx++)
    // {
    //     float2 row = RegionBounds[rowIdx];
    //     float lower = row.x;
    //     float upper = row.y;
    //
    //     // Skip rows with invalid bounds (both values near zero)
    //     float isValidRow = step(0.00001, max(abs(lower), abs(upper)));
    //
    //     // Inside region calculation
    //     float insideRegion = step(lower, uv.y) * step(uv.y, upper) * isValidRow;
    //
    //     // Lower fade region calculation
    //     float lowerLerp = lower - FadeDistance;
    //     float inLowerFade = step(lowerLerp, uv.y) * step(uv.y, lower) * isValidRow;
    //     float normalizedLowerPos = saturate((uv.y - lowerLerp) / max(FadeDistance, 0.00001)); // Avoid division by zero
    //     float lowerFadeAlpha = lerp(1.0, FadedAlpha, normalizedLowerPos);
    //
    //     // Upper fade region calculation
    //     float upperLerp = upper + FadeDistance;
    //     float inUpperFade = step(upper, uv.y) * step(uv.y, upperLerp) * isValidRow;
    //     float normalizedUpperPos = saturate((uv.y - upper) / max(FadeDistance, 0.00001)); // Avoid division by zero
    //     float upperFadeAlpha = lerp(FadedAlpha, 1.0, normalizedUpperPos);
    //
    //     // Combine results - apply each effect only where needed using lerp with the mask
    //     Alpha = lerp(Alpha, FadedAlpha, insideRegion);
    //     Alpha = lerp(Alpha, lowerFadeAlpha, inLowerFade);
    //     Alpha = lerp(Alpha, upperFadeAlpha, inUpperFade);
    // }


    // for (int rowIdx = 0; rowIdx < 3; rowIdx++)
    // {
    //     float2 row = RegionBounds[rowIdx];
    //     float lower = row.x;
    //     float upper = row.y;
    //
    //     float lowerLerp = lower - FadeDistance;
    //     float upperLerp = upper + FadeDistance;
    //
    //     // Inside region calculation
    //     float insideRegion = step(lower, uv.y) * step(uv.y, upper);
    //
    //     // Lower fade region calculation
    //     float inLowerFade = step(lowerLerp, uv.y) * step(uv.y, lower);
    //     float lowerFactor = saturate((uv.y - lowerLerp) / FadeDistance);
    //     float lowerFadeAlpha = lerp(1.0, FadedAlpha, lowerFactor);
    //
    //     // Upper fade region calculation
    //     float inUpperFade = step(upper, uv.y) * step(uv.y, upperLerp);
    //     float upperFactor = saturate((uv.y - upper) / FadeDistance);
    //     float upperFadeAlpha = lerp(FadedAlpha, 1.0, upperFactor);
    //
    //     // Combine results
    //     Alpha = lerp(Alpha, FadedAlpha, insideRegion);
    //     Alpha = lerp(Alpha, lowerFadeAlpha, inLowerFade);
    //     Alpha = lerp(Alpha, upperFadeAlpha, inUpperFade);
    // }

    // for (int rowIdx = 0; rowIdx < 4; rowIdx++)
    // {
    //     float2 row = RegionBounds[rowIdx];
    //     float lower = row.x;
    //     float upper = row.y;
    //
    //     // Easy case..we are within bounds, so just return the alpha value
    //     if (uv.y > lower && uv.y < upper)
    //     {
    //         Alpha = FadedAlpha;
    //         return;
    //     }
    //
    //     // Lerp lower
    //     float lowerLerp = lower - FadeDistance;
    //     float upperLerp = upper + FadeDistance;
    //
    //     // If we're between lowerLerp and lower, lerp from 1.0 to _FadedAlpha
    //     if (uv.y >= lowerLerp && uv.y <= lower)
    //     {
    //         // Normalize lowerLerp and lower such that v goes from 0 at lowerLerp to 1 at lower
    //         float factor = ((uv.y - lowerLerp) + 0.000001) / lower - lowerLerp;
    //         Alpha = lerp(1.0, FadedAlpha, factor);
    //         return;
    //     }
    //
    //     if (uv.y >= upper && uv.y <= upperLerp)
    //     {
    //         // Normalize upper and upperLerp such that v goes from 0 at upper to 1 at upperLerp
    //         float factor = ((uv.y - lower) + 0.000001) / upperLerp - upper;
    //         Alpha = lerp(FadedAlpha, 1.0, factor);
    //         return;
    //     }
    // }
    //
    // // If we haven't returned yet, alpha is 1.0
    // Alpha = 1.0;
}

#endif
