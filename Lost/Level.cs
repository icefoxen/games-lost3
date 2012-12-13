using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using OpenTK;

namespace Lost {
	/*
	public class LevelGenerator {
		
		// These should all take a location the shape is centered on, so we can branch them.
		// XXX: Oops... a minimum size might be a good idea.
		public Vector2d[] GenCircle(Vector2d center, double radius, int count) {
			double angle = 0.0;
			double increment = 2*Math.PI/(double) count;
			Vector2d[] v = new Vector2d[count];
			for(int i = 0; i < count; i++) {
				double dx = Math.Cos(angle) * radius;
				double dy = Math.Sin(angle) * radius;
				v[i].X = center.X + dx;
				v[i].Y = center.Y + dy;
				angle += increment;
			}
			return v;
		}
		
		public Vector2d[] GenEllipse(double rad, double ellipticness, double facing, int count) {
			return new Vector2d[0];
		}
		
		public Vector2d[] GenSpiral(Vector2d center, double radius, int count) {
			double angle = 0.0;
			double increment = 2*Math.PI/(double) count;
			double rad = 0.0;
			double rincrement = radius / (double) count;
			Vector2d[] v = new Vector2d[count];
			
			for(int i = 0; i < count; i++) {
				double dx = Math.Cos(angle) * rad;
				double dy = Math.Sin(angle) * rad;
				v[i].X = center.X + dx;
				v[i].Y = center.Y + dy;
				angle += increment;
				rad += rincrement;
			}
			return v;
		}
		
		public Vector2d[] GenSine(double length, double amplitude, int count) {
			return new Vector2d[0];
		}
		
		public Vector2d[] GenGrid(int w, int h, double spacing, int count) {
			return new Vector2d[0];
		}
		
		public Vector2d[] GenHexGrid(int w, int h, double spacing, int count) {
			return new Vector2d[0];
		}
		
		public Vector2d[] GenArc(double w, double h, int count) {
			return new Vector2d[0];
		}
		
		public Vector2d[] GenSpirograph(int count) {
			return new Vector2d[0];
		}
		
		public Level GenLevel(int rockiness, int stability, int danger) {
			return new Level("foo");
		}
		
		public void AddDanger(Level l) {
			switch(Misc.Rand.Next() % 10) {
			case 0:
				break;
			case 1:
				break;
			case 2:
				break;
			case 3:
				break;
			case 4:
				break;
			case 5:
				break;
			case 6:
				break;
			case 7:
				break;
			case 8:
				break;
			case 9:
				break;
			}
		}
		
		public void AddRockiness(Level l) {
			
		}
		
		public void AddFeature(Level l, Func<IGameObj> f) {
			IGameObj r = f();
			r.Loc = PointWithin(l.Radius);
			l.Add(r);
		}
		
		public void AddShape(Level l, Vector2d[] shape, Func<IGameObj> f) {
			foreach(Vector2d p in shape) {
				IGameObj g = f();
				g.Loc = p;
				l.Add(g);
			}
		}
		
		public void AddFeatures(Level l, int c, Func<IGameObj> f) {
			for(int IntShouldHaveAnEnumerator = 0; IntShouldHaveAnEnumerator < c; IntShouldHaveAnEnumerator++) {
				AddFeature(l, f);
			}
		}
		
		public void AddUpTo(Level l, int c, Func<IGameObj> f) {
			int i = Misc.Rand.Next() % c;
			AddFeatures(l, i, f);
		}
		
		public Vector2d PointWithin(double radius) {
			double angle = Misc.Rand.NextDouble() * 2 * Math.PI;
			Vector2d d = new Vector2d(Math.Cos(angle) * radius, Math.Sin(angle) * radius);
			return d;
		}
	}
	*/
	
	public interface ILevel {
		Mesh Background {get; set;}
		HashSet<IGameObj> Objs {get; set;}
		double Radius {get; set;}
		void Add(IGameObj o);
		void Boundary(IGameObj o);
		void Calc(GameState g);
	}
	
	public class BaseLevel : ILevel {
		
		public Mesh Background {get; set;}
		public HashSet<IGameObj> Objs {get; set;}
		public double Radius {get; set;}
		public void Add(IGameObj o) {
			Objs.Add(o);
		}
		public virtual void Boundary(IGameObj o) {
			double dist = o.Loc.Length;
			double f = dist - Radius;
			if(f > 0) {
				Vector2d pos = o.Loc;
				pos.Normalize();
				o.Push(Vector2d.Multiply(pos, -(dist / 100)));
				// And bounce them back.
			}
		}
		public virtual void Calc(GameState g) {
			
		}
	}
	
	delegate IGameObj ShipMaker(Vector2d loc, double facing);
	
	/*
	 * So we need min and max wave that it can spawn, we need to keep track of all the
	 * important objects in the wave 
	 * 
	 * Okay, so:
	 * Choose wave type and make sure it is not too high or too low
	 *    If wave is a multiple of 5, choose boss wave instead
	 * For each potential ship type in the wave,
	 * Spawn (wavenum * count) +- 25%
	 * 
	 */
	struct WaveParam {
		public ShipMaker Ship;
		public int ShipCount;
		public WaveParam(ShipMaker s, int c) {
			Ship = s;
			ShipCount = c;
		}
	}
	class WaveType {
		// Eventually having it have a cost for each ship might be good, but for now,
		// we just choose N ships.
		WaveParam[] ShipSpecs;
		const int SpawnRadius = 300;
		public int MinWave;
		public int MaxWave;
		
		public WaveType(WaveParam[] s, int min, int max) {
			ShipSpecs = s;
			MinWave = min;
			MaxWave = max;
		}
		
		public List<IGameObj> GetShips(int wavenum) {
			List<IGameObj> objs = new List<IGameObj>();
			foreach(WaveParam w in ShipSpecs) {
				// count = ShipCount * (something between 0.8 and 1.2)
				double variance = (Misc.Rand.NextDouble() * 0.4) + 0.8;
				// Never spawn less than 1
				int count = (int)Math.Round(((double)(w.ShipCount * wavenum) * variance));
				while(count > 0) {
					Vector2d loc = Misc.PointBetween(SpawnRadius, SpawnRadius + 50);
					double facing = Misc.Rand.NextDouble() * Misc.TWOPI;
					objs.Add(w.Ship(loc, facing));
					count -= 1;
				}
			}
			return objs;
		}
	}
	
	public class WaveLevel : BaseLevel {
		public int Wave { get; set; }
		long calcs = 0;
		// waveTime = 10 --> 1 minutes at 0.2 calcs per second.
		int waveTime = 6;
		int waveNumber = 1;
		//int money = 0;
		List<WaveType> WaveTypes;
		
		
		public WaveLevel() {
			XmlDocument config = Loader.GetConfig("levelwave.xml");
			
			XmlNode rad = config.SelectSingleNode("/level/radius");
			Radius = Double.Parse(rad.InnerText);
			
			XmlNode tex = config.SelectSingleNode("/level/bg");
			Background = Loader.GetBillboard(tex.InnerText);
			Objs = new HashSet<IGameObj>();
			WaveTypes = new List<WaveType>();
			
			WaveParam scouts = new WaveParam(MakeScout, 3);
			WaveParam swarmers = new WaveParam(MakeSwarmer, 5);
			WaveParam fighters = new WaveParam(MakeFighter, 3);
			WaveParam gunboats = new WaveParam(MakeGunboat, 2);
			WaveParam cruisers = new WaveParam(MakeCruiser, 2);
			WaveParam carriers = new WaveParam(MakeCarrier, 1);
			WaveParam dreadnoughts = new WaveParam(MakeDreadnought, 1);
			
			WaveParam ferals = new WaveParam(MakeFeral, 2);
			WaveParam berserkers = new WaveParam(MakeBerserker, 2);
			WaveParam spiders = new WaveParam(MakeSpider, 2);
			
			WaveParam rocks = new WaveParam(MakeRock, 4);
			WaveParam turrets = new WaveParam(MakeTurret, 2);
			WaveParam blocks = new WaveParam(MakeBlock, 2);
			
			WaveParam birds = new WaveParam(MakeBird, 3);
			
			WaveParam[] wavePeewee = new WaveParam[]{scouts};
			WaveParam[] waveSwarm = new WaveParam[] { swarmers};
			WaveParam[] waveBullets = new WaveParam[] { swarmers, gunboats };
			WaveParam[] waveSerious = new WaveParam[] { gunboats };
			WaveParam[] waveCruiser = new WaveParam[] { cruisers, gunboats };
			WaveParam[] waveCarrier = new WaveParam[] { carriers, fighters, fighters };
			WaveParam[] waveDreadnought = new WaveParam[] { dreadnoughts, };
			
			WaveParam[] waveWild = new WaveParam[] {  spiders, berserkers };
			WaveParam[] waveFeral = new WaveParam[] { ferals, berserkers };
			WaveParam[] waveTrouble = new WaveParam[] {  berserkers, spiders };
			WaveParam[] waveHazard = new WaveParam[] { rocks, blocks, turrets };
			WaveParam[] waveRocks = new WaveParam[] { rocks, rocks, rocks, blocks, rocks, rocks };
			
			WaveParam[] waveBirds = new WaveParam[] { birds, birds };
			
			//WaveTypes.Add(new WaveType(wavePeewee, 1, 100));
			//WaveTypes.Add(new WaveType(waveSwarm, 1, 100));
			WaveTypes.Add(new WaveType(waveBirds, 1, 100));
			//WaveTypes.Add(new WaveType(waveRocks, 1, 100));
			
			//WaveTypes[2] = new WaveType(waveBullets, 1, 100);
			//WaveTypes[3] = new WaveType(waveSerious, 1, 100);
			//WaveTypes[4] = new WaveType(waveCruiser, 1, 100);
			//WaveTypes[5] = new WaveType(waveCarrier, 1, 100);
			//WaveTypes[6] = new WaveType(waveDreadnought, 1, 100);
			//WaveTypes[7] = new WaveType(waveWild, 1, 100);
			//WaveTypes[8] = new WaveType(waveFeral, 1, 100);
			//WaveTypes[9] = new WaveType(waveTrouble, 1, 100);
		}
			
		// You know, a better way to do it would be to check a global timer of some kind...
		public override void Calc(GameState g) {
			if(calcs % waveTime == 0) {
				NextWave(g);
				waveNumber += 1;
			}
			Console.WriteLine("Calcs: {0}", calcs);
			calcs += 1;
		}
		
		void NextWave(GameState g) {
			Console.WriteLine("Spawning wave {0}", waveNumber);
			int wavetype = Misc.Rand.Next() % WaveTypes.Count;
			while(WaveTypes[wavetype].MinWave > waveNumber ||
				WaveTypes[wavetype].MaxWave < waveNumber) {
				wavetype = Misc.Rand.Next() % WaveTypes.Count;
			}
			List<IGameObj> objs = WaveTypes[wavetype].GetShips(waveNumber);
			g.AddObjs(objs);
		}
		
		// Yay, since C# has poor introspection and no macros, what we get
		// instead is a FUCKTON OF COPY PASTE CODE.
		// There MIGHT be a way around this, sort of.
		public IGameObj MakeTurret(Vector2d loc, double facing) {
			AIController c = new TurretController();
			Turret t = new Turret(loc, facing, c);
			t.Vel = Misc.PointBetween(1, 10);
			return t;
		}

		public IGameObj MakeRock(Vector2d loc, double facing) {
			Rock t = new Rock(loc, facing);
			t.Vel = Misc.PointBetween(1, 10);
			return t;
		}
		
		public IGameObj MakeBlock(Vector2d loc, double facing) {
			Block t = new Block(loc, facing);
			t.Vel = Misc.PointBetween(1, 10);
			return t;
		}
		
		public IGameObj MakeFan(Vector2d loc, double facing) {
			Fan t = new Fan(loc, facing);
			t.Vel = Misc.PointBetween(1, 10);
			return t;
		}
		
		public IGameObj MakeDeathTurret(Vector2d loc, double facing) {
			IController c = new TurretController();
			DeathTurret t = new DeathTurret(loc, facing, c);
			t.Vel = Misc.PointBetween(1, 10);
			t.Facing = Misc.Rand.NextDouble() * Misc.TWOPI;
			return t;
		}
		
		public IGameObj MakeScout(Vector2d loc, double facing) {
			IController c = new DumbShipController();
			Scout t = new Scout(loc, facing, c);
			t.Vel = Misc.PointBetween(1, 10);
			t.Facing = Misc.Rand.NextDouble() * Misc.TWOPI;
			return t;
		}
		
		public IGameObj MakeSwarmer(Vector2d loc, double facing) {
			IController c = new DumbShipController();
			var t = new Swarmer(loc, facing, c);
			t.Vel = Misc.PointBetween(1, 10);
			t.Facing = Misc.Rand.NextDouble() * Misc.TWOPI;
			return t;
		}
		
		public IGameObj MakeFighter(Vector2d loc, double facing) {
			IController c = new DumbShipController();
			var t = new Fighter(loc, facing, c);
			t.Vel = Misc.PointBetween(1, 10);
			t.Facing = Misc.Rand.NextDouble() * Misc.TWOPI;
			return t;
		}
		
		public IGameObj MakeGunboat(Vector2d loc, double facing) {
			IController c = new DumbShipController();
			var t = new Gunboat(loc, facing, c);
			t.Vel = Misc.PointBetween(1, 10);
			t.Facing = Misc.Rand.NextDouble() * Misc.TWOPI;
			return t;
		}
		
		public IGameObj MakeCruiser(Vector2d loc, double facing) {
			IController c = new DumbShipController();
			var t = new Cruiser(loc, facing, c);
			t.Vel = Misc.PointBetween(1, 10);
			t.Facing = Misc.Rand.NextDouble() * Misc.TWOPI;
			return t;
		}
		
		public IGameObj MakeCarrier(Vector2d loc, double facing) {
			IController c = new DumbShipController();
			var t = new Carrier(loc, facing, c);
			t.Vel = Misc.PointBetween(1, 10);
			t.Facing = Misc.Rand.NextDouble() * Misc.TWOPI;
			return t;
		}
		
		public IGameObj MakeDreadnought(Vector2d loc, double facing) {
			IController c = new DumbShipController();
			var t = new Dreadnought(loc, facing, c);
			t.Vel = Misc.PointBetween(1, 10);
			t.Facing = Misc.Rand.NextDouble() * Misc.TWOPI;
			return t;
		}
		
		public IGameObj MakeFeral(Vector2d loc, double facing) {
			IController c = new DumbShipController();
			var t = new Feral(loc, facing, c);
			t.Vel = Misc.PointBetween(1, 10);
			t.Facing = Misc.Rand.NextDouble() * Misc.TWOPI;
			return t;
		}
		
		public IGameObj MakeBerserker(Vector2d loc, double facing) {
			IController c = new DumbShipController();
			var t = new Berserker(loc, facing, c);
			t.Vel = Misc.PointBetween(1, 10);
			t.Facing = Misc.Rand.NextDouble() * Misc.TWOPI;
			return t;
		}
		
		public IGameObj MakeSpider(Vector2d loc, double facing) {
			IController c = new DumbShipController();
			var t = new Spider(loc, facing, c);
			t.Vel = Misc.PointBetween(1, 10);
			t.Facing = Misc.Rand.NextDouble() * Misc.TWOPI;
			return t;
		}
		
		public IGameObj MakeNomad(Vector2d loc, double facing) {
			IController c = new DumbShipController();
			var t = new Nomad(loc, facing, c);
			t.Vel = Misc.PointBetween(1, 10);
			t.Facing = Misc.Rand.NextDouble() * Misc.TWOPI;
			return t;
		}
		
		public IGameObj MakeBird(Vector2d loc, double facing) {
			IController c = new BirdController();
			var t = new Bird(loc, facing, c);
			t.Vel = Misc.PointBetween(1, 10);
			t.Facing = Misc.Rand.NextDouble() * Misc.TWOPI;
			return t;
		}
	}
	
	
		/* Parameters we have:
		 * Rockiness.  Number of rocks, number of meteor storms.
		 * Dangerousness.  Enemy spawn rate and such.
		 * Stability.  Less stable areas tend to make more weird stuff; bubble clouds, warp storms, etc.
		 * Lower stability may paradoxically result in more structures muddled together.
		 * 
		 * We also need to build the connection tree, drop powerups, and link levels to each other with gates.
		 * 
		 * Okay...  The level has target numbers for rockiness, dangerousness and stability.
		 * And every...  10 seconds or so it checks its status against those numbers and...
		 * Adds something(s)
		 * Removes something
		 */
	public class Level : ILevel {
		public Mesh Background {get; set;}
		public HashSet<IGameObj> Objs {get; set;}
		public List<int> Connections {get;set;}
		public double Radius {get; set;}
		public XmlDocument Config;
		
		public int Rocks;
		public int Weirdness;
		public int Danger;
		
		public int LevelNum {get; set;}
		
		public Level(int i) {
			LevelNum = i;
			Config = Loader.GetConfig("level1.xml");
			XmlNode rad = Config.SelectSingleNode("/level/radius");
			Radius = Double.Parse(rad.InnerText);
			
			//XmlNode tex = Config.SelectSingleNode("/level/bg");
			Background = Loader.GetBillboard("background.bb");
			Objs = new HashSet<IGameObj>();
			Connections = new List<int>();
		}
		
		public void AddConnection(int i) {
			Connections.Add(i);
			Gate g = new Gate(Misc.PointWithin(1), i);

			Add(g);
		}
		
		
		public void Add(IGameObj o) {
			Objs.Add(o);
		}
		
		public void Boundary(IGameObj o) {
			double dist = o.Loc.Length;
			double f = dist - Radius;
			if(f > 0) {
				Vector2d pos = o.Loc;
				pos.Normalize();
				o.Push(Vector2d.Multiply(pos, -(dist/100)));  // And bounce them back.
			}
		}
		
		// Periodically, we go through the entire level and figure out what it is
		// compared to what it should be, and then add or remove stuff to be more what
		// it should be.
		// Man, this can do SO MUCH MORE, like spawn crazy storms or secret areas.
		// It could even check for boss state and such, perhaps...
		public void Calc(GameState g) {
			int rocks = 0;
			int weirdness = 0;
			int danger = 0;
			foreach(IGameObj o in g.Objs) {
				rocks += o.Rockness;
				weirdness += o.Weirdness;
				danger += o.Dangerness;
			}
			
			if(rocks < Rocks) {
				AddRock(g);
			} else {
				DoSomethingAmbiguous(g);
			}
			if(weirdness < Weirdness) {
				AddWeirdness(g);
			} else {
				DoSomethingAmbiguous(g);
			}
			if(danger < Danger) {
				AddDanger(g);
			} else {
				DoSomethingAmbiguous(g);
			}
		}
		
		private void DoSomethingAmbiguous(GameState g) {
			int roll = Misc.Rand.Next() % 10;
			switch(roll) {
			case 0:
				AddRock(g);
				break;
			case 1:
				AddWeirdness(g);
				break;
			case 2:
				AddDanger(g);
				break;
			case 4:
				break;
			default:
				RemoveSomething(g);
				break;
			}
		}
		
		// Should ideally be a leetle more Random, but oh well.
		private void RemoveSomething(GameState g) {
			foreach(IGameObj o in g.Objs) {
				// Only non-vital objects (not players, powerups, etc) have these properties.
				if(o.Rockness > 0 || o.Weirdness > 0 || o.Dangerness > 0) {
					g.KillObj(o);
					break;
				}
			}
		}
		
		
		
		// KISS, for now.
		private void AddRock(GameState g) {
			Rock r = new Rock(Misc.PointBetween(100, Radius), 0);
			r.Vel = Misc.PointWithin(5);
			g.AddObj(r);
		}
		
		private void AddWeirdness(GameState g) {
			Fan r = new Fan(Misc.PointBetween(100, Radius), 0);
			r.Vel = Misc.PointWithin(5);
			g.AddObj(r);
		}
		
		delegate TurretController foo();
		
		private void AddDanger(GameState g) {
			Turret t = new Turret(Misc.PointBetween(100, Radius), 0, new TurretController());
			t.Vel = Misc.PointWithin(5);
			g.AddObj(t);
		}
		
		public IGameObj[] GetGates(GameState g) {
			List<IGameObj> l = new List<IGameObj>();
			foreach(IGameObj o in g.Objs) {
				if(o.GateNum >= 0) {
					l.Add(o);
				}
			}
			return l.ToArray();
		}
	}
}
