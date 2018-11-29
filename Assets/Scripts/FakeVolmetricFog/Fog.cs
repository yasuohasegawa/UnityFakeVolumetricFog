using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class Fog : MonoBehaviour {
    const int BLOCK_SIZE = 16;

    public Mesh mesh;
    public Material material;
    public ComputeShader computeShader;

    private Bounds bounds;
    private ComputeBuffer positionBuffer;
    private ComputeBuffer rotationMatrixBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5];
    private uint numeberOfQuads = 60000;

    private Vector3[] positions;
    private Vector3 maxPos = new Vector3(150f, 1f, 150f);


    void Awake()
    {
        
    }

    // Use this for initialization
    void Start()
    {
        // 12 = float (4 byte x 3）
        positionBuffer = new ComputeBuffer((int)numeberOfQuads, 12);
        rotationMatrixBuffer = new ComputeBuffer((int)numeberOfQuads, Marshal.SizeOf(typeof(Matrix4x4)));
        positions = new Vector3[numeberOfQuads];

        Matrix4x4[] mat = new Matrix4x4[numeberOfQuads];
        for (int i = 0; i < numeberOfQuads; i++)
        {
            positions[i] = new Vector3(Random.Range(-maxPos.x, maxPos.x), maxPos.y, Random.Range(-maxPos.z, maxPos.z));
            mat[i] = Matrix4x4.identity;
        }

        positionBuffer.SetData(positions);
        rotationMatrixBuffer.SetData(mat);
        material.SetBuffer("PositionBuffer", positionBuffer);
        material.SetBuffer("RotationMatrixBuffer", rotationMatrixBuffer);

        bounds = new Bounds(Vector3.zero, new Vector3(numeberOfQuads / 3, numeberOfQuads / 3, numeberOfQuads / 3));

        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        args[0] = mesh.GetIndexCount(0);
        args[1] = numeberOfQuads;
        args[2] = mesh.GetIndexStart(0);
        args[3] = mesh.GetBaseVertex(0);
        args[4] = 0;
        argsBuffer.SetData(args);
    }

    /*  // memo
     *  shader.dispatch(Kernel, 4096, 1, 1)

        and in your compute shader you have

        [numthreads(1024,1,1)]

        That will give up 4096*1024 = 4,194,304 particles in total.
    */
    void Update()
    {
        int kernelId = computeShader.FindKernel("CSMain");
        computeShader.SetBuffer(kernelId, "PositionBuffer", positionBuffer);
        computeShader.SetBuffer(kernelId, "RotationMatrixBuffer", rotationMatrixBuffer);

        int groupSize = Mathf.CeilToInt(numeberOfQuads / BLOCK_SIZE);
        computeShader.Dispatch(kernelId, groupSize, 1, 1);
        computeShader.SetFloat("_Time", Time.time);
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
    }

    void OnDestroy()
    {
        positionBuffer.Release();
        rotationMatrixBuffer.Release();
        argsBuffer.Release();
    }
}
