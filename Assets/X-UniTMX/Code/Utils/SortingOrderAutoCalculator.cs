/*! 
 * X-UniTMX: A tiled map editor file importer for Unity3d
 * https://bitbucket.org/Chaoseiro/x-unitmx
 * 
 * Copyright 2013-2014 Guilherme "Chaoseiro" Maia
 *           2014 Mario Madureira Fontes
 */
using UnityEngine;

namespace X_UniTMX.Utils
{
	/// <summary>
	/// Automatically calculates the Renderer's SortingOrder from this GameObject using _tiledMap information.
	/// </summary>
	[RequireComponent(typeof(Renderer))]
	[ExecuteInEditMode]
	public class SortingOrderAutoCalculator : MonoBehaviour
	{
		Renderer _renderer = null;
		Map _tiledMap = null;
		Vector2 _pos;
		TiledMapComponent _tiledMapComponent = null;
		/// <summary>
		/// Offsets this Transform's position to account for Pivot Points that are not centered in the sprite
		/// </summary>
		[Tooltip("Offsets this Transform's position to account for Pivot Points that are not centered in the sprite. This Offset is in Global Coordinates!")]
		public Vector2 Offset = Vector2.zero;

		void OnEnable()
		{
			_renderer = GetComponent<Renderer>();
			// Try to get TiledMap from TiledMapComponent
			
			_tiledMapComponent = GameObject.FindObjectOfType<TiledMapComponent>();
			if (_tiledMapComponent != null)
			{
				_tiledMap = _tiledMapComponent.TiledMap;
			}
		}

		/// <summary>
		/// Manually sets the Map that this script will use to calculate the SortingOrder
		/// </summary>
		/// <param name="tiledMap">Map to use to calculate SortingOrder</param>
		public void SetMap(Map tiledMap)
		{
			_tiledMap = tiledMap;
		}

		void Update()
		{
			if (_tiledMap == null && _tiledMapComponent != null)
			{
				_tiledMap = _tiledMapComponent.TiledMap;
				if (_tiledMap == null)
					return;
			}
			if (_renderer == null)
			{
				_renderer = GetComponent<SpriteRenderer>();
				if (_renderer == null)
					return;
			}
			_pos = _tiledMap.WorldPointToTileIndex((Vector2)transform.position + Offset);
			_renderer.sortingOrder = _tiledMap.GetSortingOrder(_pos.x, _pos.y);

		}
	}
}
