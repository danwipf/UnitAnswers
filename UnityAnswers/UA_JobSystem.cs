using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;


using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Jobs;

public static class UA_JobSystem 
{
        public static List<Vector3> LastSpawns;
   		public static Vector3[] TestJobOut(this Vector3[] VectorResults,SimpsonVector sv,Vector3 pos,Vector3 lastSpawn,float gap){
			var na_sv = new NativeArray<Vector3>(4,Allocator.Persistent);
			var bp_ls = new NativeArray<Vector3>(1,Allocator.Persistent);
			var bp_length = new NativeArray<int>(1,Allocator.Persistent);
			na_sv[0] = sv.A;
			na_sv[1] = sv.B;
			na_sv[2] = sv.C;
			na_sv[3] = sv.D;

			var joblength = new GetLengthJob(){
				r_SimpsonVector = na_sv,
				r_gap = gap,
				r_transformPosition = pos,
				r_LastSpawn = lastSpawn,
				w_length = bp_length
			};
			var jobhandlelenght = joblength.Schedule();
			jobhandlelenght.Complete();

			var v3 = new NativeArray<Vector3>(bp_length[0],Allocator.Persistent);

			var job = new GetSegmentsJob(){
				r_SimpsonVector = na_sv,
				r_gap = gap,
				r_transformPosition = pos,
				r_LastSpawn = lastSpawn,
				w_bp_vector3 = v3,
				w_bp_lastSpawn = bp_ls
			};
			var jobhandle = job.Schedule();
			jobhandle.Complete();

			LastSpawns = bp_ls.ToList();
            VectorResults = v3.ToArray();

			na_sv.Dispose();
			v3.Dispose();
			bp_ls.Dispose();
			bp_length.Dispose();

			return VectorResults;
		}
        public struct GetLengthJob : IJob{
        [ReadOnly] public NativeArray<Vector3> r_SimpsonVector;
        [ReadOnly] public float r_gap;
        [ReadOnly] public Vector3 r_transformPosition;
        [ReadOnly] public Vector3 r_LastSpawn;
        [WriteOnly] public NativeArray<int> w_length;

        public void Execute(){
            List<Vector3> _VectorResults = new List<Vector3>();
            Vector3 LastSpawn = r_LastSpawn;
            float step = 0.1f;
            float t = step;
            float lastT = new float();

            while (t >= 0 && t <= 1f)
            {
                while (t < 1f && Vector3.Distance(GetPoint(r_SimpsonVector,t),LastSpawn) < r_gap){
                    t += step;
                }
                step /= 10f;
                while (t > lastT && Vector3.Distance(GetPoint(r_SimpsonVector,t),LastSpawn) > r_gap){
                    t -= step;
                }
                step /= 10f;
                if(t > 1f || t < lastT){
                    break;
                }
                if(step < 0.000001f){
                    LastSpawn = GetPoint(r_SimpsonVector,t);
                    _VectorResults.Add(LastSpawn + r_transformPosition);
                    lastT = t;
                    step = 0.1f;
                }
            }
           w_length[0] = _VectorResults.Count;
        }
            public static Vector3 GetPoint(NativeArray<Vector3> Input,float t){
                t = Mathf.Clamp01(t);
                float OneMinusT = 1 - t;
                return  
                    Mathf.Pow( OneMinusT,3) * Input[0] +
                    3f * Mathf.Pow(OneMinusT,2) * t * Input[1] +
                    3f * OneMinusT * Mathf.Pow(t,2) * Input[2] +
                    Mathf.Pow(t,3) * Input[3];
        }
    }
    public struct GetSegmentsJob : IJob{
        [ReadOnly] public NativeArray<Vector3> r_SimpsonVector;
        [ReadOnly] public float r_gap;
        [ReadOnly] public Vector3 r_transformPosition;
        [ReadOnly] public Vector3 r_LastSpawn;
        [WriteOnly] public NativeArray<Vector3> w_bp_vector3;
        [WriteOnly] public NativeArray<Vector3> w_bp_lastSpawn;
        

        public void Execute(){
            List<Vector3> _VectorResults = new List<Vector3>();
            List<Vector3> bp_ls = new List<Vector3>();
            Vector3 LastSpawn = r_LastSpawn;
            float step = 0.1f;
            float t = step;
            float lastT = new float();

            while (t >= 0 && t <= 1f)
            {
                while (t < 1f && Vector3.Distance(GetPoint(r_SimpsonVector,t),LastSpawn) < r_gap){
                    t += step;
                }
                step /= 10f;
                while (t > lastT && Vector3.Distance(GetPoint(r_SimpsonVector,t),LastSpawn) > r_gap){
                    t -= step;
                }
                step /= 10f;
                if(t > 1f || t < lastT){
                    break;
                }
                if(step < 0.000001f){
                    LastSpawn = GetPoint(r_SimpsonVector,t);
                    _VectorResults.Add(LastSpawn + r_transformPosition);
                    lastT = t;
                    step = 0.1f;
                }
            }
            w_bp_lastSpawn[0] = LastSpawn;
            w_bp_vector3.CopyFrom(_VectorResults.ToArray());
        }
        
            
        
    
        public static Vector3 GetPoint(NativeArray<Vector3> Input,float t){
            t = Mathf.Clamp01(t);
            float OneMinusT = 1 - t;
            return  
                Mathf.Pow( OneMinusT,3) * Input[0] +
                3f * Mathf.Pow(OneMinusT,2) * t * Input[1] +
                3f * OneMinusT * Mathf.Pow(t,2) * Input[2] +
                Mathf.Pow(t,3) * Input[3];
        }
    }
}
