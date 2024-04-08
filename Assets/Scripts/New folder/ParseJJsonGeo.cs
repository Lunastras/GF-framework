using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class ParseJJsonGeo : MonoBehaviour
{
    public GameObject MarkerPrefab;

    public RectTransform MapTransform;

    public Vector2 MapSize;

    public CityMarker Marker1;

    public CityMarker Marker2;

    public CityMarker Marker3;

    public Vector2 GetMercatorCoords(Coordinates aCoords)
    {
        float lat = aCoords.lat;
        float lon = aCoords.lon;

        float latRad = lat * Mathf.Deg2Rad;
        float mercN = Mathf.Log(Mathf.Tan(Mathf.PI / 4f + latRad / 2f));

        float x = (lon + 180) * (MapSize.x / 360);
        float y = (MapSize.y / 2) - (MapSize.x * mercN / (2 * Mathf.PI));

        return new(x, y);
    }

    // Start is called before the first frame update
    void Start()
    {
        string dataPath = "D:/bkp/NeededGeoData.txt";

        Cities cities = JsonUtility.FromJson<Cities>(File.ReadAllText(dataPath));

        CityInfo[] data = cities.results;
        for (int i = 0; i < data.Length; ++i)
        {
            int population = data[i].population;
            CityMarker markerData;
            if (population < 5000000)
            {
                markerData = Marker1;
            }
            else if (population < 10000000)
            {
                markerData = Marker2;
            }
            else
            {
                markerData = Marker3;
            }

            GameObject marker = Instantiate(MarkerPrefab);
            marker.GetComponent<Image>().color = markerData.Colour;
            RectTransform markerTransform = marker.GetComponent<RectTransform>();
            markerTransform.SetSizeWithCurrentAnchors(markerData.Diameter, markerData.Diameter);
            markerTransform.localPosition = GetMercatorCoords(data[i].coordinates);
            markerTransform.SetParent(MapTransform);
        }
    }
}

[System.Serializable]
public class Cities
{
    public CityInfo[] results;
}

[System.Serializable]
public struct CityInfo
{
    public int population;
    public Coordinates coordinates;
}

[System.Serializable]
public struct Coordinates
{
    public float lat;
    public float lon;
}

[System.Serializable]
public struct CityMarker
{
    public float Diameter;
    public Color Colour;
}
