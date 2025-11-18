using UnityEngine;

public class MapDisplay : MonoBehaviour
{

    public Renderer textureRender;

    //Renders texture
    public void DrawTexture(Texture2D texture)
    {

        textureRender.sharedMaterial.mainTexture = texture;
    }

}