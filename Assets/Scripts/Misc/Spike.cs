using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D))]
public class Spike : MonoBehaviour {
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
	
	void Awake()
	{
		_posicaoInicial = new Vector3(transform.position.x, transform.position.y, transform.position.z);
		_caixaInicial = GetComponent<BoxCollider2D>().bounds;
		_moverPara.Set(_posicaoInicial.x, _posicaoInicial.y - _caixaInicial.size.y / MovimentarEmUnidades, _posicaoInicial.z);
		_movimentoHabilitado = false;

		StartCoroutine(HabilitarMovimento(((float)Random.Range(0, 200)) / 100));
	}
	
	// Update is called once per frame
	void Update () {
	
		_movendoPosicaoFinal = MovendoPosicaoFinal;

		if (_movimentoHabilitado){

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
					MovendoPosicaoFinal = true;
				}
			}

		}

		if (MovendoPosicaoFinal && !_movendoPosicaoFinal)
		{
			_movimentoHabilitado = false;
			StartCoroutine(HabilitarMovimento(MoverPosicaoInicialSegundos));
		}
		else if (!MovendoPosicaoFinal && _movendoPosicaoFinal)
		{
			_movimentoHabilitado = false;
			StartCoroutine(HabilitarMovimento(MoverPosicaoFinalSegundos));
		}
	}

	public IEnumerator HabilitarMovimento(float segundos)
	{
		Debug.Log(segundos);
		yield return new WaitForSeconds(segundos);
		_movimentoHabilitado = true;
	}
}
