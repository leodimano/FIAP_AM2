using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteRendererTileMap : MonoBehaviour
{

    public Sprite TopLeft;
    public Sprite Top;
    public Sprite TopRight;
    public Sprite CenterLeft;
    public Sprite Center;
    public Sprite CenterRight;
    public Sprite BottomLeft;
    public Sprite Bottom;
    public Sprite BottomRight;

    SpriteRenderer _spriteRenderer;

    // Use this for initialization
    void Awake()
    {
        RenderTileMap();
    }

    private void RenderTileMap()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

        Vector2 _spriteSize = new Vector2(_spriteRenderer.bounds.size.x / transform.localScale.x,
                                          _spriteRenderer.bounds.size.y / transform.localScale.y);

        // Cria o 'PreFab' do Tile
        GameObject _childPrefab = new GameObject();
        SpriteRenderer _childSprite = _childPrefab.AddComponent<SpriteRenderer>();
        _childPrefab.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        _childSprite.sprite = _spriteRenderer.sprite;

        // Cria o ponteiro dos clones
        GameObject _childObject;

        Sprite _sprite;

        for (int indexX = 0, listX = (int)Mathf.Round(_spriteRenderer.bounds.size.x) - 1; indexX <= listX; indexX++)
        {
            for (int indexY = 0, listY = (int)Mathf.Round(_spriteRenderer.bounds.size.y) - 1; indexY <= listY; indexY++)
            {
                _sprite = null;

                Vector3 _tilePosition = new Vector3(transform.position.x - (_spriteSize.x * indexX * -1),
                                                    transform.position.y - (_spriteSize.y * indexY),
                                                    transform.position.z);

                if (indexY == 0)
                {
                    if (indexX == 0)
                        _sprite = TopLeft;
                    else if (indexX == listX)
                        _sprite = TopRight;
                    else
                        _sprite = Top;
                }
                else if (indexY == listY)
                {
                    if (indexX == 0)
                        _sprite = BottomLeft;
                    else if (indexX == listX)
                        _sprite = BottomRight;
                    else
                        _sprite = Bottom;
                }
                else
                {
                    if (indexX == 0)
                        _sprite = CenterLeft;
                    else if (indexX == listX)
                        _sprite = CenterRight;
                    else
                        _sprite = Center;
                }

                if (_sprite == null)
                    _sprite = Center;

                _childObject = Instantiate(_childPrefab) as GameObject;
                _childObject.transform.position = _tilePosition;
                _childObject.transform.parent = transform;
                _childObject.GetComponent<SpriteRenderer>().sprite = _sprite;
                _childObject.GetComponent<SpriteRenderer>().color = _spriteRenderer.color;
                _childObject.isStatic = true;
            }
        }

        GameObject.Destroy(_childPrefab);

        _spriteRenderer.enabled = false;
    }

}
