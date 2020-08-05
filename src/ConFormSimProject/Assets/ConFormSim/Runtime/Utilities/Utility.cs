using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;


public static class Utility
{
    /// <summary>
    /// Checks if the given position is occupied or free. A bounding box is used to
    /// check if there are any other colliders at the position. This bounding box is 
    /// described with position, rotation and scale.
    /// </summary>
    /// <param name="position"> Position to be checked </param>
    /// <param name="rotation"> Rotation of the used bounding box </param>
    /// <param name="halfExtents"> 	Half of the size of the box in each dimension. </param>
    /// <param name="tags"> Object tags which describe an obstacle </param>
    /// <param name="triggerInteraction"> Defines whether the position is also checked 
    /// for triggers (Collide) or if those are ignored </param>
    /// <returns> 
    /// Array of colliders of gameObjects that have one of the tags in the 
    /// area described with position, scale and rotation.
    /// </returns>
    public static Collider[] OccupiedPosition(
        Vector3 position, 
        Quaternion rotation, 
        Vector3 halfExtents, 
        List<string> tags,
        QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore)
    {
        LayerMask allLayers = ~0;
        Collider[] hitColliders = Physics.OverlapBox(
            position, 
            halfExtents, 
            rotation,
            allLayers,
            queryTriggerInteraction: triggerInteraction);
        Collider[] obstacles = hitColliders
            .Where(col => (tags.Contains(col.gameObject.tag)))
            .ToArray();
        return obstacles;
    }

    /// <summary>
    /// Calculates the bounds that encapsulate all child objects and their childs of 
    /// an object. This is particularly useful if you need the world scale of an 
    /// object consisting of multiple children and grand children.
    /// </summary>
    /// <param name="parent"> 
    /// The game object whose bounds should be calculated. 
    /// </param>
    /// <returns> The bounds describing the bounding box of the parent </returns>
    public static Bounds GetBoundsOfAllDeepChilds(GameObject parent)
    {
        if (parent.transform.childCount > 0)
        {
            // First find a center for your bounds.
            Vector3 center = Vector3.zero;
            // number of children that have a renderer component
            int renderChildCount = 0;   

            Renderer childRenderer;
            foreach (Transform child in parent.transform.Cast<Transform>().ToArray())
            {
                if (child.childCount > 0)
                {
                    center += GetBoundsOfAllDeepChilds(child.gameObject).center;
                    renderChildCount++;
                }
                else
                {
                    if (child.TryGetComponent<Renderer>(out childRenderer))
                    {
                        center += childRenderer.bounds.center;
                        renderChildCount++;
                    }
                }
            }

            //center is average center of children
            center /= renderChildCount; 

            // Now you have a center, calculate the bounds by creating a zero
            // sized 'Bounds' 
            Bounds bounds = new Bounds(center, Vector3.zero);

            foreach (Transform child in parent.transform)
            {
                if (child.childCount > 0)
                {
                    bounds.Encapsulate(GetBoundsOfAllDeepChilds(child.gameObject));
                }
                else
                {
                    if (child.TryGetComponent<Renderer>(out childRenderer))
                    {
                        bounds.Encapsulate(childRenderer.bounds);
                    }
                }
            }
            return bounds;
        }
        else
        {
            Renderer renderer;
            if (parent.TryGetComponent<Renderer>(out renderer))
            {
                return renderer.bounds;
            }
            else
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }
            
        }
        
    }

    /// <summary>
    /// Calculates the bounds that encapsulate all child objects of an object.
    /// But it doesn't take care of any grand children. This is particularly useful 
    /// if you only need the first child generation in world scale.
    /// </summary>
    /// <param name="parent"> 
    /// The game object whose bounds should be calculated. 
    /// </param>
    /// <returns> The bounds describing the bounding box of the parent </returns>
    public static Bounds GetBoundsOfAllChilds(GameObject parent)
    {
        if (parent.transform.childCount > 0)
        {
            // First find a center for your bounds.
            Vector3 center = Vector3.zero;

            Renderer childRenderer;
            foreach (Transform child in parent.transform.Cast<Transform>().ToArray())
            {
                if (child.TryGetComponent<Renderer>(out childRenderer))
                {
                    center += childRenderer.bounds.center;
                }

            }

            //center is average center of children
            center /= parent.transform.childCount; 

            // Now you have a center, calculate the bounds by creating a zero
            // sized 'Bounds' 
            Bounds bounds = new Bounds(center, Vector3.zero);

            foreach (Transform child in parent.transform)
            {
                if (child.TryGetComponent<Renderer>(out childRenderer))
                {
                    bounds.Encapsulate(childRenderer.bounds);
                }
            }
            return bounds;
        }
        else
        {
            Renderer renderer;
            if (parent.TryGetComponent<Renderer>(out renderer))
            {
                return renderer.bounds;
            }
            else
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }
            
        }
        
    }

    /// <summary>
    /// Returns every single bound of an objects and childs attached to its
    /// transform.
    /// </summary>
    /// <param name="parent">The object to get the bounds from</param>
    /// <returns>List of all bounds of the parent object and its
    /// childs.</returns>
    public static List<Bounds> GetBoundsOfAllDeepChildObjects(GameObject parent)
    {
        List<Bounds> allBounds = new List<Bounds>();

        foreach(Transform child in parent.transform)
        {
            // add all bounds of this childs child objects
            allBounds.AddRange(GetBoundsOfAllDeepChildObjects(child.gameObject));
        }
        // add bounds off the parent object
        Renderer renderer;
        if (parent.TryGetComponent<Renderer>(out renderer))
        {
            allBounds.Add(renderer.bounds);
        }
        return allBounds;
    }

    /// <summary>
    /// Calculates a int32 number from a Color32 object. Every byte represents a
    /// color. The format is 0xRRGGBBAA --> red is 0xFF000000.
    /// </summary>
    /// <param name="col"> 
    /// The color to be converted
    /// </param>
    /// <returns> The int32 representing the color. </returns>
    public static int GetIntFromColor(Color32 col)
    {
        byte[] rgba = new byte[] {col.r, col.g, col.b, col.a};
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(rgba);
        }
        int hexColor = BitConverter.ToInt32(rgba, 0);
        return hexColor;
    }

    /// <summary>
    /// Calculates a Color32 object from a int32 number. Every byte of the number
    /// represents a color. The format is 0xRRGGBBAA --> red is 0xFF000000.
    /// </summary>
    /// <param name="col"> 
    /// The number representing the color.
    /// </param>
    /// <returns> The color object represented by the number. </returns>
    public static Color32 GetColorFromInt(int hexColor)
    {   
        byte[] rgba = BitConverter.GetBytes(hexColor);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(rgba);
        }
        Color32 col = new Color32();
        col.r = rgba[0];
        col.g = rgba[1];
        col.b = rgba[2];
        col.a = rgba[3];
        return col;
    }

    private static System.Random rng = new System.Random(); 
    /// <summary>
    /// Shuffles a list randomly. Usage: 
    /// <br></br>List<t> myList = new List<T>();
    /// <br></br>myList.Shuffle();
    /// </summary>
    public static void Shuffle<T>(this IList<T> list)
    {
      int n = list.Count;
      while (n > 1)
      {
        n--;
        int k = rng.Next(n + 1);
        T value = list[k];
        list[k] = list[n];
        list[n] = value;
      }
    }

    /// <summary>
    /// A C# implementation of the Marsaglia polar method to create a normally
    /// distributed random values. 
    /// Credits to oferei and  Oneiros90 in the unity forums:
    /// https://answers.unity.com/questions/421968/normal-distribution-random.html
    /// </summary>
    /// <returns>random gaussian number</returns>
    public static float NextGaussian()
    {
        float u, v, S;
        
            do
            {
                u = 2.0f * UnityEngine.Random.value - 1.0f;
                v = 2.0f * UnityEngine.Random.value - 1.0f;
                S = u * u + v * v;
            }
            while (S >= 1.0f);
        
            // Standard Normal Distribution
            float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);
            return std;
    }

    /// <summary>
    /// Calculates a random number from a Gaussian distribution and clamps it
    /// according to the 3 sigma rule.
    /// Credits to oferei and  Oneiros90 in the unity forums:
    /// https://answers.unity.com/questions/421968/normal-distribution-random.html
    /// </summary>
    /// <param name="minValue">Min value (inclusive)</param>
    /// <param name="maxValue">Max value (inclusive)</param>
    /// <returns>Random Gaussian number within a range.</returns>
    public static float RandomGaussian(float minValue = 0.0f, float maxValue = 1.0f)
    {
        // Standard Normal Distribution
        float std = NextGaussian();
    
        // Normal Distribution centered between the min and max value
        // and clamped following the "three-sigma rule"
        float mean = (minValue + maxValue) / 2.0f;
        float sigma = (maxValue - mean) / 3.0f;
        return Mathf.Clamp(std * sigma + mean, minValue, maxValue);
    }
}