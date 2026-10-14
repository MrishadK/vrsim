using UnityEngine;

public class TextureScroller : MonoBehaviour
{
    public Vector2 scrollSpeed = new Vector2(0f, 0.5f);
    private Material escalatorMaterial;

    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            escalatorMaterial = rend.material; 
        }
    }

    void Update()
    {
        if (escalatorMaterial != null)
        {
            escalatorMaterial.mainTextureOffset += scrollSpeed * Time.deltaTime;
        }
    }
}