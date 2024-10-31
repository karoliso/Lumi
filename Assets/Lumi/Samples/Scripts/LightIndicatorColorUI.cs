using UnityEngine;
using UnityEngine.UI;

namespace Lumi
{
    public class LightIndicatorColorUI : MonoBehaviour
    {
        [SerializeField] private LightDetector lightDetector;
        [SerializeField] private Image lightIndicator;
        [SerializeField] private AnimationCurve illuminationCurve;

        private void Update()
        {
            if (lightDetector == null)
            {
                return;
            }

            float illumination = lightDetector.SampledLightAmount;
            float illuminationAdjusted = illuminationCurve.Evaluate(Mathf.Clamp01(illumination));

            if (lightIndicator != null)
            {
                lightIndicator.color = illuminationAdjusted * Color.white;
            }
        }
    }
}