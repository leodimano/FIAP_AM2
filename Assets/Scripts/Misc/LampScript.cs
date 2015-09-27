using System;
using UnityEngine;

[Serializable]
[RequireComponent(typeof(Light))]
public class LampScript : MonoBehaviour
{
    /// <summary>
    /// Liga ou desliga a variacao de Cor por T
    /// </summary>
    public bool ColorVariance;
    /// <summary>
    /// Tempo de variação da luz, utilizando no lerp para alteração da cor
    /// </summary>
    public float ColorVarianceT;
    /// <summary>
    /// Cor que o lerp irá atingir;
    /// </summary>
    public Color ColorVarianceRGBA;

    /// <summary>
    /// Liga ou Desliga a variação de intensidade da Luz por T
    /// </summary>
    public bool IntensityVariance;
    /// <summary>
    /// Tempo de variacao da Luz, utilizando no lerp da intensidade
    /// </summary>
    public float IntensityVarianceT;
    /// <summary>
    /// Valor da nova intensidade para interpolacao
    /// </summary>
    public float NewIntensityVariance;

    /// <summary>
    /// Liga ou desliga a variacao do raio da luz por T
    /// </summary>
    public bool RadiusVariance;
    /// <summary>
    /// Tempo de variacao do raio, utilizando lerp do raio
    /// </summary>
    public float RadiusVarianceT;
    /// <summary>
    /// Valor do novo raio para interpolacao
    /// </summary>
    public float NewRadiusVariance;


    private Light _lightComponente;
    private Color _defaultLightColor;
    private float _defaultIntensity;
    private float _defaultRadius;
    private float _newIntensityVariance;
    private float _newRadiusVariance;
    private bool _lerpingToColorVariance = true;
    private bool _lerpingToIntensityVariance = true;
    private bool _lerpingToRadiusVariance = true;

    public void Awake()
    {
        _lightComponente = GetComponent<Light>();
        _defaultLightColor = _lightComponente.color;
        _defaultIntensity = _lightComponente.intensity;
        _defaultRadius = _lightComponente.spotAngle;
    }

    // Update is called once per frame
    void Update()
    {
        if (ColorVariance)
        {
            if (_lerpingToColorVariance)
            {
                _lightComponente.color = Color.Lerp(_lightComponente.color, ColorVarianceRGBA, ColorVarianceT * Time.deltaTime);

                if (_lightComponente.color == ColorVarianceRGBA)
                {
                    _lerpingToColorVariance = false;
                }
            }
            else
            {
                _lightComponente.color = Color.Lerp(_lightComponente.color, _defaultLightColor, ColorVarianceT * Time.deltaTime);

                if (_lightComponente.color == _defaultLightColor)
                {
                    _lerpingToColorVariance = true;
                }
            }
        }

        if (IntensityVariance)
        {
            if (_lerpingToIntensityVariance)
            {
                _newIntensityVariance = NewIntensityVariance + UnityEngine.Random.Range(-0.5f, 0.5f);
            }
            else
            {
                _newIntensityVariance = _defaultIntensity + UnityEngine.Random.Range(-0.5f, 0.5f);
            }

            _lightComponente.intensity = Mathf.Lerp(_lightComponente.intensity, _newIntensityVariance, IntensityVarianceT * Time.deltaTime);

            if ((_lightComponente.intensity > _defaultIntensity && _lightComponente.intensity < NewIntensityVariance) ||
                (_lightComponente.intensity < _defaultIntensity && _lightComponente.intensity > NewIntensityVariance))
            {
                _lerpingToIntensityVariance = !_lerpingToIntensityVariance;
            }
        }

        if (RadiusVariance)
        {
            if (_lerpingToRadiusVariance)
            {
                _newRadiusVariance = NewRadiusVariance + UnityEngine.Random.Range(-5f, 5f);
            }
            else
            {
                _newRadiusVariance = _defaultRadius + UnityEngine.Random.Range(-5f, 5f);
            }

            //_newRadiusVariance = Mathf.Clamp(_newRadiusVariance, 1, 179);

            _lightComponente.spotAngle = Mathf.LerpAngle(_lightComponente.spotAngle, _newRadiusVariance, RadiusVarianceT * Time.deltaTime);

            if ((_lightComponente.spotAngle > _defaultRadius && _lightComponente.spotAngle < NewRadiusVariance) ||
                (_lightComponente.spotAngle < _defaultRadius && _lightComponente.spotAngle > NewRadiusVariance))
            {
                _lerpingToRadiusVariance = !_lerpingToRadiusVariance;
            }
        }
    }
}
