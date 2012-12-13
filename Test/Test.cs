using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTK;
using Lost;
namespace Lost.Test {
	
	public class Test {

		public static void CompareCollision(int n, double radius) {
			Console.WriteLine("Running with {0} objects in a radius of {1} units", n, radius);
			IGameObj[] gob = new IGameObj[n];
			for(int i = 0; i < n; i++) {
				gob[i] = new DummyObj(Misc.PointWithin(radius), 0);
			}
			
			long collisions = 0;
			Stopwatch time = new Stopwatch();
			time.Start();
			for(int i = 0; i < gob.Length; i++) {
				IGameObj o = gob[i];
				// The j=i is why we don't use foreach here...
				for(int j = i; j < gob.Length; j++) {
					IGameObj go = gob[j];
					if(o.Id < go.Id && o.Colliding(go)) {
						collisions += 1;
					}
				}
			}
			time.Stop();
			Console.WriteLine("Brute force found {0} collisions in {1} ms", collisions, time.ElapsedMilliseconds);
			
			Func<IGameObj, Vector2d> f = delegate(IGameObj o) {
				return o.Loc;
			};
			
		
			collisions = 0;
			Stopwatch time2 = new Stopwatch();
			time.Reset();
			time.Start();
			time2.Start();
			KdTree<IGameObj> k = KdTree<IGameObj>.BuildByInsertion(f, gob);
			time2.Stop();
			foreach(IGameObj o in gob) {
				ICollection<IGameObj> c = k.GetWithin(o.Loc, 50);
				foreach(IGameObj go in c) {
					// This conveniently prevents things from colliding twice or
					// colliding with themselves.
					if(o.Id > go.Id && o.Colliding(go)) {
						collisions += 1;
					}
				}
			}
			time.Stop();
			Console.WriteLine("kd-tree found {0} collisions in {1} ms, of which {2} ms was building the tree", 
				collisions, time.ElapsedMilliseconds, time2.ElapsedMilliseconds);
			
			
			collisions = 0;
			time.Reset();
			time2.Reset();
			time.Start();
			time2.Start();
			QuadTree<IGameObj> q = QuadTree<IGameObj>.Build(f, gob);
			time2.Stop();
			foreach(IGameObj o in gob) {
				ICollection<IGameObj> c = q.GetWithin(o.Loc, 50);
				foreach(IGameObj go in c) {
					// This conveniently prevents things from colliding twice or
					// colliding with themselves.
					if(o.Id > go.Id && o.Colliding(go)) {
						collisions += 1;
					}
				}
			}
			time.Stop();
			Console.WriteLine("quadtree found {0} collisions in {1} ms, of which {2} ms was building the tree", 
				collisions, time.ElapsedMilliseconds, time2.ElapsedMilliseconds);
			
			//Console.WriteLine(k);
			
		}
		
		
		public static void DoCollisionCompare() {
			CompareCollision(100, 1000);
			
			CompareCollision(1000, 1000);
			CompareCollision(10000, 1000);
			CompareCollision(20000, 1000);
			Console.WriteLine("******************************************");
			
			CompareCollision(100, 2500);
			CompareCollision(1000, 2500);
			CompareCollision(10000, 2500);
			CompareCollision(20000, 2500);
			Console.WriteLine("******************************************");
			
			CompareCollision(100, 5000);
			CompareCollision(1000, 5000);
			CompareCollision(10000, 5000);
			CompareCollision(20000, 5000);
			
			
		}
		
		public static void Main() {
			DoCollisionCompare();
		}
	}
}

