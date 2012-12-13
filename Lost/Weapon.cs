using System;
using OpenTK;
namespace Lost {
	public enum ShotMode {
		CYCLE,
		BURST
	}
	
	public delegate IGameObj MakeShot(Vector2d loc, double facing);
	
	public interface IWeapon {
		int RefireRate { get; set; }
		double EnergyCost { get; set; }
		ShotMode Mode { get; set; }
		WeaponPoint[] Points { get; set; }
		
		// Takes amount of energy, checks that it's okay, returns new energy amount
		double Fire(GameState g, double energy, Vector2d loc, double direction, Vector2d vel);
		void Calc(GameState g);	
	}
	
	public struct WeaponPoint {
		Vector2d FirePoint;
		Vector2d FireDirection;
		int BulletCount;
		double Deviation;
		double ShotVel;
		MakeShot ShotFunc;
		
		public WeaponPoint(MakeShot shotfunc, Vector2d point, Vector2d dir, double dev, double vel, int count) {
			FirePoint = point;
			FireDirection = dir;
			Deviation = dev;
			ShotVel = vel;
			BulletCount = count;
			ShotFunc = shotfunc;
		}
		
		public void Fire(GameState g, Vector2d loc, double facing, Vector2d vel) {
			
			Vector2d firePoint = Misc.Vector2dRotate(FirePoint, facing);
			firePoint = Vector2d.Add(firePoint, loc);
			
			for(int i = 0; i < BulletCount; i++) {
				double deviation = (Misc.Rand.NextDouble() * Deviation) - (Deviation / 2);
				double speedDeviation = (Misc.Rand.NextDouble() * Deviation * 5);
				Vector2d fireDir = Misc.Vector2dRotate(FireDirection, facing + deviation);
				IGameObj s = ShotFunc(firePoint, facing + deviation);
				s.Vel = Vector2d.Multiply(fireDir, ShotVel+speedDeviation);
				s.Vel = Vector2d.Add(s.Vel, vel);
				g.AddObj(s);
			}
		}
	}
	
	public class BaseWeapon : IWeapon {
		public ShotMode Mode { get; set; }
		public int RefireRate { get; set; }
		int ReloadState { get; set; }
		int NextShot { get; set; }
		public double EnergyCost { get; set; }
		public WeaponPoint[] Points { get; set; }
		
		public BaseWeapon(int refire, double energy, WeaponPoint[] p, ShotMode m) {
			ReloadState = 0;
			NextShot = 0;
			EnergyCost = energy;
			
			RefireRate = refire;
			Points = p;
			Mode = m;
		}
		
		public double Fire(GameState g, double energy, Vector2d loc, double facing, Vector2d vel) {
			if(ReloadState > 0 || energy < EnergyCost) {
				return energy;
			}
			ReloadState = RefireRate;
			
			switch(Mode) {
			case ShotMode.CYCLE:
				Points[NextShot].Fire(g, loc, facing, vel);
				NextShot = (NextShot + 1) % Points.Length;
				break;
			case ShotMode.BURST:
				foreach(WeaponPoint p in Points) {
					p.Fire(g, loc, facing, vel);
				}
				break;
			}
			return energy - EnergyCost;
		}
		
		public void Calc(GameState g) {
			ReloadState = Math.Max(0, ReloadState - 1);
		}
	}
	
	public class WeaponFactory {
		static IGameObj MakeVulcanShot(Vector2d loc, double facing) {
			return new VulcanShot(loc, facing);
		}
		
		static IGameObj MakeGrenadeShot(Vector2d loc, double facing) {
			return new GrenadeShot(loc, facing);
		}
		
		static IGameObj MakeRocketShot(Vector2d loc, double facing) {
			IController c = new MissileController();
			return new Rocket(loc, facing, c);
		}
		
		static IGameObj MakePulsarShot(Vector2d loc, double facing) {
			return new PulsarShot(loc, facing);
		}
		
		static IGameObj MakeShootgunShot(Vector2d loc, double facing) {
			return new ShootgunShot(loc, facing);
		}
		
		static IGameObj MakePhantomHammerShot(Vector2d loc, double facing) {
			return new PhantomHammerShot(loc, facing);
		}
		
		static IGameObj MakeFireballShot(Vector2d loc, double facing) {
			return new FireballShot(loc, facing);
		}
		
		static IGameObj MakeRailgunShot(Vector2d loc, double facing) {
			return new RailgunShot(loc, facing);
		}
		
		// Basically, these functions are shortcut interfaces to produce uniform
		// weapon points.  Fire rate and fire mode still depends on the Weapon
		// object though.
		// Shot type, fire point, direction of fire, spread, initial velocity, number of shots
		public static WeaponPoint VulcanPoint(Vector2d point, Vector2d dir) {
			return new WeaponPoint(MakeVulcanShot, point, dir, Misc.PIOVER2/4, 10, 2);
		}
		
		public static WeaponPoint GrenadePoint(Vector2d point, Vector2d dir) {
			return new WeaponPoint(MakeGrenadeShot, point, dir, Misc.PIOVER2/2, 6, 3);
		}
		
		public static WeaponPoint RocketPoint(Vector2d point, Vector2d dir) {
			return new WeaponPoint(MakeRocketShot, point, dir, 0.1, 1, 1);
		}
		
		public static WeaponPoint PulsarPoint(Vector2d point, Vector2d dir) {
			return new WeaponPoint(MakePulsarShot, point, dir, 0.0, 0, 1);
		}
		
		public static WeaponPoint PulseBeamPoint(Vector2d point, Vector2d dir) {
			return new WeaponPoint(MakePulsarShot, point, dir, 0.05, 0, 3);
		}
		
		public static WeaponPoint ShootgunPoint(Vector2d point, Vector2d dir) {
			return new WeaponPoint(MakeShootgunShot, point, dir, 0.6, 15, 20);
		}
		
		public static WeaponPoint PhantomHammerPoint(Vector2d point, Vector2d dir) {
			return new WeaponPoint(MakePhantomHammerShot, point, dir, 0.0, 0, 1);
		}
		
		public static WeaponPoint FireballPoint(Vector2d point, Vector2d dir) {
			return new WeaponPoint(MakeFireballShot, point, dir, 0.1, 15, 1);
		}
		
		public static WeaponPoint RailgunPoint(Vector2d point, Vector2d dir) {
			return new WeaponPoint(MakeRailgunShot, point, dir, 0.02, 25.0, 1);
		}
	}
}

