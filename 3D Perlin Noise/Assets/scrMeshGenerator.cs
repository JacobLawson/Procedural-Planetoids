using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class scrMeshGenerator : MonoBehaviour
{
    [SerializeField]
    private Vector3 worldDimensions;
    [SerializeField]
    private GameObject testCube;

    //Perlin Variables
    [SerializeField]
    private float Lacunarity = 2;
    [SerializeField]
    private float Persistance = 0.5f;
    [SerializeField]
    private float scale = 2;
    [SerializeField]
    private float limitations = 4f;

    //Mesh 'Chunk' Generation
    private Mesh m_mesh;
    private Vector3[] vertices;
    private int[] triangles;
   

    void Start()
    {
        //Create a new mesh that can be altered
        m_mesh = new Mesh();
        //Attatch the new mesh to the object
        GetComponent<MeshFilter>().mesh = m_mesh;

        //Define new Mesh
        CreateMesh();
        //Update Mesh
        //UpdateMesh();
    }

    void CreateMesh()
    {
        vertices = new Vector3[((int)worldDimensions.x + 1) * ((int)worldDimensions.y + 1) * ((int)worldDimensions.z + 1)];
        triangles = new int[((int)worldDimensions.x * ((int)worldDimensions.y) * (int)worldDimensions.z) * 6];

        for (int i = 0, z = 0; z <= (int)worldDimensions.z; z++)
        {
            for (int y = 0; y <= (int)worldDimensions.y; y++)
            {
                for (int x = 0; x <= (int)worldDimensions.x; x++)
                {
                    
                    if (GeneratePerlin(x, y, z) >= .5f)
                    {
                        //vertices[i] = new Vector3(x, y,, z);
                        Instantiate(testCube, new Vector3(x, y, z), Quaternion.identity);
                        //Instantiate(testCube, GeneratePerlin(x, 0, z), Quaternion.identity);
                    }
                    i++;
                }
            }           
        }
    
        for (int vert = 0, tris = 0, z = 0; z < (int)worldDimensions.z; z++)
        {
            for (int y = 0; y < (int)worldDimensions.y; y++)
            {
                for (int x = 0; x < (int)worldDimensions.x; x++)
                {
                    triangles[tris + 0] = vert + 0;
                    triangles[tris + 1] = vert + (int)worldDimensions.x + 1;
                    triangles[tris + 2] = vert + 1;
                    triangles[tris + 3] = vert + 1;
                    triangles[tris + 4] = vert + (int)worldDimensions.x + 1;
                    triangles[tris + 5] = vert + (int)worldDimensions.x + 2;

                    vert++;
                    tris += 6;
                }
                vert++;
            }           
        }
    }

    void UpdateMesh()
    {
        GetComponent<MeshFilter>().mesh.Clear();

        GetComponent<MeshFilter>().mesh.vertices = vertices;
        GetComponent<MeshFilter>().mesh.triangles = triangles;
    }

    float GeneratePerlin(float a_x, float a_y, float a_z)
    {
        float AB = Mathf.PerlinNoise(a_x, a_y);
        float BC = Mathf.PerlinNoise(a_y, a_z);
        float AC = Mathf.PerlinNoise(a_x, a_z);

        float BA = Mathf.PerlinNoise(a_y, a_x);
        float CB = Mathf.PerlinNoise(a_z, a_y);
        float CA = Mathf.PerlinNoise(a_z, a_x);

        float ABC = (AB + BC + AC + BA + CB + CA) / 6;

        return ABC;
    }
}
