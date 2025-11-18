using UnityEngine;

public static class TextureGenerator
{

    public static Texture2D TextureFromColorMap(Color[] colorMap, int width, int height)
    {
        //Texture settings
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Trilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        //Applies colormap
        texture.SetPixels(colorMap);
        texture.Apply();
        return texture;
    }


    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {

        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        //Creates new colormap array and loops through pixels on map
        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                //Sets pixels to black and white
                colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
            }
        }

        return TextureFromColorMap(colourMap, width, height);
    }

}