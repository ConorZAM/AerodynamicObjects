using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AngleOfAttackSegment : MonoBehaviour
{
    //MeshFilter mf;
    [Range(-180,180)]
    public float includedAngleDeg;
    [Range(1,5)]
    public float radius;
    [Range(2,10)]
    public int numberOfPoints;
    List<Vector3> verticesList = new List<Vector3> { };
    List<int> trianglesList = new List<int> { };
    List<Vector3> normalsList = new List<Vector3> { };
    Vector3[] vertices;
    Mesh mesh;
    AeroBody aeroBody;

    // Start is called before the first frame update
    void Awake()
    {
        aeroBody = GetComponent<AeroBody>();

        Transform angleOfAttackWedge = new GameObject().transform;
        angleOfAttackWedge.name = "Angle of Attack Wedge";
        angleOfAttackWedge.parent = transform;
        MeshFilter mf=angleOfAttackWedge.gameObject.AddComponent<MeshFilter>();
        MeshRenderer mr= angleOfAttackWedge.gameObject.AddComponent<MeshRenderer>();
        mesh = new Mesh();
        mf.mesh = mesh;
        mr.materials[0].color = ArrowSettings.angleOfAttackWedgeColour;
        mr.materials[0].ToFadeMode();

        //Vertices

        float angle = 0;
        // add zero point
        verticesList.Add(Vector3.zero);
        //at radial points
        for (int i = 0; i < numberOfPoints; i++)
        {
            
            angle = ((float)i / (numberOfPoints - 1)) * includedAngleDeg * Mathf.Deg2Rad;
            float z = radius * Mathf.Cos(angle);
            float y = radius * Mathf.Sin(angle);
            verticesList.Add(new Vector3(0, y, z));

        }

        vertices = verticesList.ToArray();
        

        //triangles
        //front face
        for (int i = 0; i < (numberOfPoints - 1); i++)
        {
            trianglesList.Add(0);
            trianglesList.Add(i + 1);
            trianglesList.Add(i + 2);
        }
        //back face
        for (int i = 0; i < (numberOfPoints - 1); i++)
        {
            trianglesList.Add(i + 2);
            trianglesList.Add(i + 1);
            trianglesList.Add(0);
                       
        }


        int[] triangles = trianglesList.ToArray();

        //normals
        //front face
        for (int i = 0; i < vertices.Length; i++)
        {
            normalsList.Add(Vector3.forward);
        }
          
        Vector3[] normals = normalsList.ToArray();

        //initialise
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;

       
    }

    // Update is called once per frame
    void Update()
    {
        float includedAngle = aeroBody.alpha;
        float angle = 0;
        for (int i = 0; i < numberOfPoints; i++)
        {

            angle = -((float)i / (numberOfPoints - 1)) * includedAngle ;
            float z = radius * Mathf.Cos(angle);
            float y = radius * Mathf.Sin(angle);
            vertices[i+1]=new Vector3(0, y, z);

        }

        mesh.vertices = vertices;

    }
}
