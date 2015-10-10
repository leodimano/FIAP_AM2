using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(BoxCollider2D))]
public class Spike : MonoBehaviour
{
    public float MoverPosicaoInicialSegundos;
    public float PosicaoInicialT;
    public float MoverPosicaoFinalSegundos;
    public float PosicaoFinalT;
    public bool MovendoPosicaoFinal;
    public float MovimentarEmUnidades = 1.0f;

    private bool _movendoPosicaoFinal;
    private bool _movimentoHabilitado;
    private Vector3 _posicaoInicial;
    private Bounds _caixaInicial;
    private Vector3 _moverPara;

    private AudioSource _audioSource;

    private float _tempoInicio;
    private float _coolDown;
    private bool _iniciou;

    void Awake()
    {
        _posicaoInicial = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        _caixaInicial = GetComponent<BoxCollider2D>().bounds;
        _audioSource = GetComponent<AudioSource>();
        _moverPara.Set(_posicaoInicial.x, _posicaoInicial.y - _caixaInicial.size.y / MovimentarEmUnidades, _posicaoInicial.z);
        _movimentoHabilitado = false;

        _coolDown = 0;
        _tempoInicio = Random.Range(0f, 200f);
        _iniciou = false;

        //StartCoroutine(HabilitarMovimento(((float)Random.Range(0, 200)) / 100));
    }

    // Update is called once per frame
    // Removido o uso de Coroutines pelo consumo excessivo de memoria;
    void Update()
    {
        if (_iniciou)
        {
            _movendoPosicaoFinal = MovendoPosicaoFinal;

            if (_movimentoHabilitado)
            {

                if (MovendoPosicaoFinal)
                {
                    transform.position = Vector3.Lerp(transform.position, _posicaoInicial, PosicaoInicialT);

                    if (transform.position == _posicaoInicial)
                    {
                        MovendoPosicaoFinal = false;
                    }
                }
                else
                {
                    transform.position = Vector3.Lerp(transform.position, _moverPara, PosicaoFinalT);

                    if (transform.position == _moverPara)
                    {
                        PlayAudio();
                        MovendoPosicaoFinal = true;
                    }
                }
            }

            if (!_movimentoHabilitado &&
                ((_coolDown >= MoverPosicaoInicialSegundos && MovendoPosicaoFinal) ||
                 (_coolDown >= MoverPosicaoFinalSegundos && !MovendoPosicaoFinal)))
            {
                _movimentoHabilitado = true;
                _coolDown = 0;
            }
            else
            {
                _coolDown += Time.time;
            }

            if (MovendoPosicaoFinal && !_movendoPosicaoFinal)
            {
                _movimentoHabilitado = false;
            }
            else if (!MovendoPosicaoFinal && _movendoPosicaoFinal)
            {
                _movimentoHabilitado = false;
            }
        }
        else
        {
            _coolDown += Time.time;

            if (_coolDown > _tempoInicio)
            {
                _coolDown = 0;
                _iniciou = true;
                _movimentoHabilitado = true;
            }
        }
    }

    public IEnumerator HabilitarMovimento(float segundos)
    {
        yield return new WaitForSeconds(segundos);
        _movimentoHabilitado = true;
    }

    private void PlayAudio()
    {
        if (_audioSource != null)
        {
            _audioSource.Stop();
            _audioSource.Play();
        }
    }
}
