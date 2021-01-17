using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeSphereFace : MonoBehaviour {
    //Declare Face Variables.
    ShapeProperties m_faceProperties;
    private Mesh m_mesh;
    private int m_resolution;
    private Vector3 m_localUp;
    private Vector3 m_axisA;
    private Vector3 m_axisB;

    private Vector3[] m_vertices;
    private int[] m_triangles;

    public CubeSphereFace(ShapeProperties a_properties, Mesh a_mesh, int a_resolution, Vector3 a_localUp) {
        //Initialise variables
        this.m_faceProperties = a_properties;
        this.m_mesh = a_mesh;
        this.m_resolution = a_resolution;
        this.m_localUp = a_localUp;

        //Get space local to face so that we don't project changes in the wrong direction.
        m_axisA = new Vector3(a_localUp.y, a_localUp.z, a_localUp.x);
        //Get the Cross product so that we can calculate a correct point on rounded face later.
        m_axisB = Vector3.Cross(m_localUp, m_axisA);
    }

    public void GenerateMesh() {
        //Create an initial array for verticies and triangles generated so that if we wish to apply changes later it will not effect the stored members of class.
        Vector3[]   initialVerticies = new Vector3[m_resolution * m_resolution];
        int[]       initialTris = new int[(m_resolution - 1) * (m_resolution - 1) * 6];

        //Initial Verticies of Face
        int triIndex = 0;
        for (int i = 0, y = 0; y < m_resolution; y++) {
            for (int x = 0; x < m_resolution; x++) {
                //Calculate the point in space of this vertex on a spheracal plane.
                Vector2 percent = new Vector2(x, y) / (m_resolution - 1);
                Vector3 pointOnCubeFace = m_localUp + (percent.x - 0.5f) * 2 * m_axisA + (percent.y - 0.5f) * 2 * m_axisB;
                Vector3 pointOnSphere = pointOnCubeFace.normalized;
                //Apply perlin Noise to add variation to the cubesphere face.
                initialVerticies[i] = m_faceProperties.GeneratePointOnShape(pointOnSphere);

                //Calculate Faces on the Face of Cubesphere.
                //Check to make sure triangle won't extend over edge of face.
                if (x != m_resolution - 1 && y != m_resolution - 1) {
                    initialTris[triIndex] = i;
                    initialTris[triIndex + 1] = i + m_resolution + 1;
                    initialTris[triIndex + 2] = i + m_resolution;
                    initialTris[triIndex + 3] = i;
                    initialTris[triIndex + 4] = i + 1;
                    initialTris[triIndex + 5] = i + m_resolution + 1;
                    triIndex += 6;
                }
                i++;
            }
        }
        //Set the faces variables now they have been calculated.
        m_vertices = initialVerticies;
        m_triangles = initialTris;
        UpdateMesh();
    }

    public void UpdateMesh() {
        //set the new mesh as this faces mesh.
        m_mesh.Clear();
        m_mesh.vertices = m_vertices;
        m_mesh.triangles = m_triangles;
        m_mesh.RecalculateNormals();
    }
    
    //Return the number of vertexes on the face mesh.
    public int VertexCount() { return m_vertices.Length; }
}

public class ShapeProperties {
    //ShapeProperties Class stores the properties of the Cubesphere making it easier to access for other class.
    //Declare variables.
    private string m_name;
    private int m_radius;
    private Transform m_origin;
    private int m_vertexCount;
    private int m_variationChunks;
    private Material m_material;

    //Allows an almost endless number of octaves to be applied to planet generation.
    public List<NoiseLayer> m_noiseLayers;

    public ShapeProperties(string a_name, Transform a_origin, int a_radius, Material a_material) {
        //Initialise variables.
        m_name = a_name;
        m_radius = a_radius;
        m_origin = a_origin;
        m_material = a_material;
        m_noiseLayers = new List<NoiseLayer>();
        m_variationChunks = 6;
    }

    public Vector3 GeneratePointOnShape(Vector3 a_spherePoint) {
        //If we have applied octaves to the generation use them to calculate the shape of the cubesphere.
        if (m_noiseLayers != null) {
            //Define initial frequency and elevation of the planet. (think of it as a sin wave we are can alter amplitude and frequency)
            float freq = 0;
            float elevation = 0;
            for (int i = 0; i <= m_noiseLayers.Count; i++) {
                //calculate frequency and amplitude based on the persistance and lacunarity set for the octave.
                float frequency = m_noiseLayers[0].GetFrequency(i);
                float amplitude = m_noiseLayers[0].GetAmplitude(i);
                freq *= frequency;
                //Calculate elevation of hills and valleys.
                elevation = m_noiseLayers[0].EvaluateVertexPoint((a_spherePoint), m_vertexCount, m_variationChunks);
                elevation = elevation * amplitude;
            }
            //Return the final point calculated.
            return a_spherePoint * m_radius * ((elevation + 1) * (freq + 1));
        }
        else {
            //Return the final point calculated.
            return a_spherePoint * m_radius;
        }      
    }

    //Getters and setters
    public string GetName() { return m_name; }
    public Transform GetOrigin() { return m_origin; }
    public Material GetMaterial() { return m_material; }
    public int GetRadius() { return m_radius; }

    public void SetVerticiesCount(int a_vertexCount) { m_vertexCount = a_vertexCount; }
    public void SetNoiseOctaves(List<NoiseLayer> a_noiseLayers) { m_noiseLayers = a_noiseLayers; }

    public void AddNoiseOctave(float a_lacunarityOfOctave, float a_persistanceOfOctave, float a_scaleFactorMin, float a_scaleFactorMax, float a_variationMin, float a_variationMax) {
        //Adds a new layer of noise the generation of this planet.
        NoiseLayer temp = new NoiseLayer(a_lacunarityOfOctave, a_persistanceOfOctave, a_scaleFactorMin, a_scaleFactorMax, a_variationMin, a_variationMax);
        m_noiseLayers.Add(temp);
    }
}

public class Noise {
    //This class contains noise functions for cubesphere.
    //Declare Variables for noise functions.
    private float m_scaleFactor;
    private float m_variation;
    private float m_variationMin;
    private float m_variationMax;
    private int m_variationChunks = 0;
    private int m_vertexChunkCount = 0;

    public Noise(float a_scaleFactorMin, float a_scaleFactorMax, float a_variationMin, float a_variationMax) {
        //Initialise variables.
        m_variationMin = a_variationMin;
        m_variationMax = a_variationMax;
        m_scaleFactor = Random.Range(a_scaleFactorMin, a_scaleFactorMax);
        m_variation   = Random.Range(m_variationMin, m_variationMax);
    }

    public float Evaluate(Vector3 a_poinOnMesh, int a_vertexCount, int a_vertexDivisions) {
        //Evaluate the point on the sphere to produce a point on sphere
        
        //Remove symetry by regenerating based on chunks as naturally faces want to mirror each other.
        if (Mathf.Floor(m_vertexChunkCount / a_vertexDivisions) == 1) {
             m_variation = Random.Range(m_variationMin, m_variationMax);
        }
        m_vertexChunkCount++;

        //Get the Coords of point
        float x = a_poinOnMesh.z, y = a_poinOnMesh.y, z = a_poinOnMesh.x;

        //Generate perlin values of point for 3D perlin Noise
        float AB = Mathf.PerlinNoise(x, y);
        float BC = Mathf.PerlinNoise(y, z);
        float AC = Mathf.PerlinNoise(x, z);

        float BA = Mathf.PerlinNoise(y, x);
        float CB = Mathf.PerlinNoise(z, y);
        float CA = Mathf.PerlinNoise(z, x);

        //Add and subtract values together and work out min and max perlin range of sphere
        float ABC = Mathf.PerlinNoise((((AB - BC - AC - BA - CB - CA) * m_scaleFactor) - m_variation), ((BA + CB + CA + AB + BC + CA) * m_scaleFactor) + m_variation);
        //Return Result
        return ABC;
    }
}

public class NoiseLayer {
    //Having noise layers allows you to layer noise upon the cubesphere to add detail using lacunarity and persistance variables.
    private Noise m_noise;
    private float m_lacunarity;
    private float m_persistance;

    public NoiseLayer(float a_lacunarity, float a_persistance, float a_scaleFactorMin, float a_scaleFactorMax, float a_variationMin, float a_variationMax) {
        //Initialise variables.
        m_lacunarity = a_lacunarity;
        m_persistance = a_persistance;
        //Create this layers noise functionality.
        m_noise = new Noise(a_scaleFactorMin, a_scaleFactorMax, a_variationMin, a_variationMax);
    }  
    
    public float EvaluateVertexPoint(Vector3 a_pointOnMesh, int a_vertexCount, int a_vertexDivisions) {
        //Use this layers noise function to evaluate where vertex should sit on mesh.
        float noiseValue = (m_noise.Evaluate(a_pointOnMesh, a_vertexCount, a_vertexDivisions) + 1) * 0.5f;
        return noiseValue;
    }
    //Get Amplitude and frequency of the layer.
    public float GetAmplitude(int a_contribution) { return Mathf.Pow(m_persistance, a_contribution); }
    public float GetFrequency(int a_contribution) { return Mathf.Pow(m_lacunarity, a_contribution); }
}

public class CubeSphere : MonoBehaviour {
    //The Cubesphere Object class.
    //Declare varaibles.
    private ShapeProperties m_planetProperties;
    private Transform m_origin;
    private MeshFilter[] m_meshFaces;
    private CubeSphereFace[] m_terrainFaces;
    private int m_numberOfFaces = 6;
    private int m_resolution;
    private int m_radius;

    private Vector3[] m_directions;

    public CubeSphere(string a_name, Transform a_origin, int a_radius, int a_resolution, Material a_material) {
        //Initialise cubesphere object.
        m_planetProperties = new ShapeProperties(a_name, a_origin, a_radius, a_material);   //create properties class for this cubesphere.
        m_resolution = a_resolution;
        //Resolution check
        if (a_resolution == 0) {
            m_resolution = 10;
        }
        else if (a_resolution > 255) {
            a_resolution = 255;
        }

        //Create arrays and vectors to define the faces and mesh creation of cubesphere.
        m_meshFaces = new MeshFilter[m_numberOfFaces];
        m_terrainFaces = new CubeSphereFace[m_numberOfFaces];
        m_directions = new Vector3[] {
            Vector3.up,
            Vector3.down,
            Vector3.left,
            Vector3.right,
            Vector3.forward,
            Vector3.back
        };
    }

    public void InitialiseCubeSphere() {
        //Create faces for the cubesphere
        for (int i = 0; i < m_numberOfFaces; i++) {
            if (m_meshFaces[i] == null) {
                GameObject meshObj = new GameObject("Mesh");
                meshObj.transform.parent = m_planetProperties.GetOrigin();
                meshObj.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Standard"));
                m_meshFaces[i] = meshObj.AddComponent<MeshFilter>();
                m_meshFaces[i].sharedMesh = new Mesh();
            }
            m_terrainFaces[i] = new CubeSphereFace(m_planetProperties, m_meshFaces[i].sharedMesh, m_resolution, m_directions[i]);
        }
    }

    public void GenerateMesh() {
        m_planetProperties.SetVerticiesCount((m_resolution * m_resolution) * 6);

        //Generate a mesh for every face of cubesphere.
        foreach (CubeSphereFace face in m_terrainFaces) {
            face.GenerateMesh();
        }
        //Create an array to merge the faces into one mesh.
        CombineInstance[] facesToBeMerged = new CombineInstance[m_meshFaces.Length];
        //Place meshes into array.
        for (int i = 0; i < m_meshFaces.Length; i++) {
            facesToBeMerged[i].mesh = m_meshFaces[i].sharedMesh;
            facesToBeMerged[i].transform = m_meshFaces[i].transform.localToWorldMatrix;
            if (i != 0) {
                Destroy(m_meshFaces[i].gameObject);
            }
        }
        //Combine mesh and rename finished cubesphere object.
        m_meshFaces[0].GetComponent<MeshFilter>().mesh = new Mesh();
        m_meshFaces[0].GetComponent<MeshFilter>().mesh.CombineMeshes(facesToBeMerged);
        m_meshFaces[0].name = m_planetProperties.GetName();
        //Apply Material
        m_meshFaces[0].GetComponent<Renderer>().material = m_planetProperties.GetMaterial();
    }

    //Get properties of the Cubesphere
    public ShapeProperties GetProperties() { return m_planetProperties; }
}

public class scrPlanetGeneration : MonoBehaviour {
    //Decalare varables that can be edited in unity.
    [SerializeField]
    int resolution;
    [SerializeField]
    int planetRadius;
    [SerializeField]
    Material material;
    //Declare variables.
    CubeSphere m_planet;

    void Start() {
        //Create a new Cubesphere in code.
        m_planet = new CubeSphere("Planet", gameObject.transform, planetRadius, resolution, material);
        m_planet.InitialiseCubeSphere();
        m_planet.GetProperties().AddNoiseOctave(2.0f, 0.5f, 50.0f, 2.0f, 1.0f, 2.0f);
        m_planet.GetProperties().AddNoiseOctave(2.0f, 0.5f, 50.0f, 2.0f, 1.0f, 2.0f);
        m_planet.GetProperties().AddNoiseOctave(2.0f, 0.25f, 50.0f, 2.0f, 1.0f, 2.0f);
        m_planet.GenerateMesh();
    }
}