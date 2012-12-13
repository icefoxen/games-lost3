using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;


namespace Lost {	
	public interface IGameObj {
		// Attributes
		long Id { get; } // And ID is guaranteed to be unique and not change over the object's lifetime.
		double EffectRange {get;set;} // This is how close something needs to be before the object cares about it
		Vector2d OldLoc {get; set;}
		Vector2d Loc {get; set;}
		double OldFacing {get; set;}
		double Facing {get; set;}
		Vector2d Vel {get; set;}
		double RVel { get; set; }
		double RDragAmount {get;set;}
		double Mass {get; set;}
		double Moment {get; set;}
		Collider Collider { get; set; }
		int Hits { get; set; }
		int MaxHits {get; set;}
		// True if bullets interact with this object.
		// False for various objects, like, many bullets.
		bool Shootable { get; set; }
		// True if colliding with something bounces both parties apart...
		bool Impactable {get;set;}
		
		// Operations
		void Push(Vector2d force);
		void Rotate(double torque);
		void Drag(double d);
		void RDrag();
		void RDrag(double d);
		void ClampVel(double d);
		
		// Collision
		bool Colliding(IGameObj o);
		void OnCollide(IGameObj o, GameState g);
		void Calc(GameState g);
		void Draw(double dt);
		void Die(GameState g);
		void Damage(int i);
		void Heal(int i);
		
		// Level generation
		int Rockness {get;set;}
		int Weirdness {get;set;}
		int Dangerness {get;set;}
		// If the object is a gate, it will return a number >0.
		int GateNum {get;set;}
	}
	
	// Objects that implement IControllable can be run by Controllers... Input module, AI, etc. 
	public interface IControllable : IGameObj {
		double MaxVel { get; set; }
		IGameObj Target { get; set; }
		double Energy { get; set; }
		double MaxEnergy { get; set; }
		void Thrust();
		void TurnLeft();
		void TurnRight();
		void Brake();
		void Fire(GameState g);
		void Secondary(GameState g);
		void Special(GameState g);
		void SwitchMain();
		void SwitchSecondary();
		void SwitchSpecial();
	}
	
	public class BaseObj : IGameObj {
		public double EffectRange { get; set; }
		public Vector2d OldLoc {get;set;}
		public Vector2d Loc {get; set;}
		
		public Vector2d Vel {get; set;}
		
		public double OldFacing {get;set;}
		public double Facing {get; set;}
		public double RVel {get; set;}
		public double Mass{get; set;}
		public double Moment { get; set; }
		public double DragAmount { get; set; }
		public double RDragAmount { get; set; }
		
		public Collider Collider{get; set;}
		
		public bool Alive{get; set;}
		public int Hits { get; set; }
		public int MaxHits { get; set; }
		// Vulnerable means it takes damage.  Invulnerable means it does not.
		public bool Vulnerable {get;set;}
		
		public int Rockness {get;set;}
		public int Weirdness {get;set;}
		public int Dangerness {get;set;}
		public int GateNum { get; set; }
		public bool Shootable { get; set; }
		public bool Impactable { get; set; }
		
		protected Mesh Mesh;
		
		// Technically we should not need (nor probably have) a setter, but
		// C# is pesky.
		public long Id { get; set; }
		
		public BaseObj(Vector2d loc, double facing) {
			Loc = loc;
			OldLoc = loc;
			Facing = facing;
			OldFacing = facing;
			Id = Misc.GetID();
			Collider = new CircleCollider(loc, 10);
			
			Mesh = Loader.GetMesh("rock.mesh");
			Moment = 20.0;
			Mass = 100.0;
			Hits = 100;
			MaxHits = Hits;
			Vulnerable = true;
			DragAmount = 0.9;
			RDragAmount = 0.8;
			
			Rockness = 0;
			Weirdness = 0;
			Dangerness = 0;
			GateNum = -1;
			Shootable = true;
			Impactable = true;
			EffectRange = 20;
		}
		
		// Operations
		public virtual void Push(Vector2d force) {
			Vector2d force2;
			force2 = Vector2d.Divide(force, Mass);
			Vel = Vector2d.Add(Vel, force2);
		}
		public virtual void Rotate(double torque) {
			RVel += torque / Moment;
		}
		public virtual void Drag() {
			Drag(DragAmount);
		}
		public virtual void Drag(double d) {
			Vel = Vector2d.Multiply(Vel, d);
		}
		public virtual void RDrag() {
			RDrag(RDragAmount);
		}
		public virtual void RDrag(double d) {
			RVel *= d;
		}
		
		public virtual void ClampVel(double d) {
			if(Vel.LengthSquared > (d * d)) {
				Vel = Vector2d.NormalizeFast(Vel);
				Vel = Vector2d.Multiply(Vel, d);
			}
		}
		
		// Collision
		// This one checks whether things are actually intersecting.
		public virtual bool Colliding(IGameObj p) {
			return Collider.Colliding(p.Collider);
		}
		
		// And this gets overridden to handle anything that needs
		// to happen on collision.  The actual collision physics
		// is handled in Misc.DoCollision.
		public virtual void OnCollide(IGameObj p, GameState g) {
			
		}
		
		protected virtual void CalcPhysics(GameState g) {
			Loc = Vector2d.Add(Loc, Vel);
			Facing += RVel;
			ClampVel(Misc.PHYSICSMAXSPEED);
			g.GetLevel().Boundary(this);
			//Console.WriteLine("Loc: {0}, Vel: {1}, Facing: {2}, RVel: {3}",
			//                  Loc, Vel, Facing, RVel);
		}

		protected virtual void CalcCollision(GameState g, ICollection<IGameObj> nearbyObjects) {
			foreach(IGameObj go in nearbyObjects) {
				// The ID check conveniently prevents things from colliding twice or
				// colliding with themselves.
				if(Id > go.Id && Colliding(go)) {
					if(Impactable && go.Impactable) {
						Misc.DoCollision(this, go);
					}
					this.OnCollide(go, g);
					go.OnCollide(this, g);
				}
			}
		}

		// The ordering here MIGHT be a problem someday, since CalcCollision() updates the
		// object's locations and such in more or less interleaved order.
		// A better way MIGHT be to do all the physics and calc, THEN do all the collisions...
		// but in practice I cannot see any difference at all.
		public virtual void Calc(GameState g) {
			OldLoc = new Vector2d(Loc.X, Loc.Y);
			OldFacing = Facing;
			ICollection<IGameObj> nearbyObjects = g.GetObjectsWithin(Loc, EffectRange);
			CalcCollision(g, nearbyObjects);
			CalcPhysics(g);
			Collider.Location = Loc;
			if(Hits > 0) {
			} else {
				//Console.WriteLine("Something dead!");
				g.KillObj(this);
			}
		}
		
		public virtual void Draw(double dt) {
			// XXX: This is a leetle bit of a hack, since the only thing with a null mesh is
			// the DummyObj used for testing...
			if(Mesh != null) {
				Vector2d lerploc = Misc.LerpVector2d(OldLoc, Loc, dt);
				double lerpfacing = Misc.LerpDouble(OldFacing, Facing, dt);
				Vector3d v = new Vector3d(lerploc);
				Quaterniond q = Quaterniond.FromAxisAngle(Graphics.OutOfScreen, lerpfacing);
				Mesh.Draw(v, q);
			}
		}
		public virtual void Die(GameState g) {
			//Console.WriteLine("Dying");
		}
		
		public virtual void Damage(int i) {
			if(Vulnerable) {
				Hits -= i;
			}
		}
		public virtual void Heal(int i) {
			Hits = Math.Min(Hits + i, MaxHits);
		}
	}
	
	public class BaseShip : BaseObj, IControllable {
		public IController Control;
		protected double ThrustForce;
		protected double TurnForce;
		protected double BrakeDrag;
		protected bool Thrusting = false;
		public double MaxVel { get; set; }
		public IGameObj Target { get; set; }
		public double Energy { get; set; }
		public double MaxEnergy { get; set; }
		public double EnergyRegen { get; set; }


		protected BaseWeapon[] Weapons;
		protected int WeaponIndex;
		
		public BaseShip(Vector2d loc, double facing) : this(loc, facing, new DummyController()) {
		}
		
		public BaseShip(Vector2d loc, double facing, IController ai) : base(loc, facing) {
			Collider = new CircleCollider(loc, 10);
			Control = ai;
			Control.Controlled = this;
			Target = null;
			
			Moment = 20.0;
			Mass = 100.0;
			Hits = MaxHits = 100;
			Vulnerable = true;
			ThrustForce = 10.0;
			DragAmount = 0.9;
			TurnForce = 1.0;
			RDragAmount = 0.8;
			BrakeDrag = 0.90;
			MaxVel = 10;
			
			Hits = MaxHits = 100;
			MaxEnergy = Energy = 100;
			EnergyRegen = 1;
			
			
			Vector2d[] firepoints = new Vector2d[2];
			firepoints[0].X = 20;
			firepoints[0].Y = 2;
			firepoints[1].X = 20;
			firepoints[1].Y = -2;
			Vector2d[] firedirs = new Vector2d[2];
			firedirs[0].X = 1;
			firedirs[0].Y = 0;
			firedirs[1].X = 1;
			firedirs[1].Y = 0;
			WeaponPoint[] weaponpoints = new WeaponPoint[3];
			weaponpoints[0] = WeaponFactory.VulcanPoint(firepoints[0], firedirs[0]);
			weaponpoints[1] = WeaponFactory.VulcanPoint(firepoints[1], firedirs[1]);
			Weapons = new BaseWeapon[1];
			Weapons[0] = new BaseWeapon(0, 2, weaponpoints, ShotMode.CYCLE);
		}
		// XXX: Thrust, Turn and Brake should just set flags that are checked in Calc
		// That way we can never Thrust or Turn more than once!
		// We might also want to make it possible for them to range from 0 to 1, for the
		// sake of steering being easier...
		public virtual void Thrust() {
			Vector2d force;
			force = Vector2d.Multiply(Misc.Vector2dFromDirection(Facing), ThrustForce);
			Push(force);
			ClampVel(MaxVel);
			Thrusting = true;
		}
		public virtual void TurnLeft() {
			Rotate(TurnForce);
		}
		public virtual void TurnRight() {
			Rotate(-TurnForce);
		}
		public virtual void Brake() {
			Drag(BrakeDrag);
		}
		public virtual void Fire(GameState g) {
			Energy = Weapons[WeaponIndex].Fire(g, Energy, Loc, Facing, Vel);
		}
		public virtual void Secondary(GameState g) {
		}
		public virtual void Special(GameState g) {
		}
		
		public virtual void SwitchMain() {
			WeaponIndex = (WeaponIndex + 1) % Weapons.Length;
		}
		
		public virtual void SwitchSecondary() {
		}
		
		public virtual void SwitchSpecial() {
		}
		
		public override void Calc(GameState g) {
			
			OldLoc = new Vector2d(Loc.X, Loc.Y);
			OldFacing = Facing;
			ICollection<IGameObj> nearbyObjects = g.GetObjectsWithin(Loc, EffectRange);
			CalcCollision(g, nearbyObjects);
			CalcPhysics(g);
			Collider.Location = Loc;
			
			if(Hits > 0) {
			} else {
				//Console.WriteLine("Something dead!");
				g.KillObj(this);
			}
			// All weapons refresh fire rate even when they're not selected, which I THINK is the right thing to do.
			foreach(IWeapon w in Weapons) {
				w.Calc(g);
			}
			Control.Calc(g, nearbyObjects);
			RDrag();
			Energy = Math.Min(Energy + EnergyRegen, MaxEnergy);
			// XXX: We need a more efficient way of doing this, ye pony gods...
			if(Thrusting) {
				Mesh[] m = new Mesh[4];
				m[0] = Loader.GetBillboard("engine-spark1.bb");
				m[1] = Loader.GetBillboard("engine-spark2.bb");
				m[2] = Loader.GetBillboard("engine-spark3.bb");
				m[3] = Loader.GetBillboard("engine-spark4.bb");
				for(int i = 0; i < 6; i++) {
					int mesh = Misc.Rand.Next(m.Length);
					Particle p = new Particle(Loc, 0, m[mesh]);
					p.Vel = new Vector2d(Misc.Rand.NextDouble() * 2 + 3, Misc.Rand.NextDouble());
					p.Vel = Misc.Vector2dRotate(p.Vel, Facing + Math.PI);
					p.Hits = 15;
					g.AddParticle(p);
				}
			}
			Thrusting = false;
			//Console.WriteLine("Loc: {0}", Loc);
		}
	}
	
	#region Misc
	public class Gate : BaseObj {
		public Gate(Vector2d loc, double facing) : base(loc, facing) {
			Collider = new Collider();
			Mass = 1000000;
			Mesh = Loader.GetMesh("gate.mesh");
			RVel = -0.01;
			Vulnerable = false;
			GateNum = 0;
		}
		
		public Gate(Vector2d loc, double facing, int t) : this(loc, facing) {
			GateNum = t;
		}
		
		// Gates are immobile.
		// Gates rotate at a fixed rate regardless of outside force.
		public override void Calc(GameState g) {
			base.Calc(g);
			Vel = Vector2d.Zero;
			
		}
		
		// Gates do not collide, you pass 'over' them.
		public override bool Colliding(IGameObj p) {
			return false;
		}
	}
	
	public class Resource : BaseObj {
		public Resource(Vector2d loc, double facing) : base(loc, facing) {}
		
	}
	public class Powerup : BaseObj {
		public Powerup(Vector2d loc, double facing) : base(loc, facing) {
			
		}
	}
	
	public class ForcefieldGenerator : BaseObj {
		public ForcefieldGenerator(Vector2d loc, double facing) : base(loc, facing) {
			Weirdness = 1;
			Dangerness = 1;
		}
		
	}
	#endregion
	
	#region Hazards
	public class Rock : BaseObj	{
		public Rock(Vector2d loc, double facing) : base(loc, facing) {
			Collider = new CircleCollider(loc, 10);
			Mass = 10;
			Hits = 1;
			MaxHits = Hits;
			RVel = Misc.Rand.NextDouble() - 0.5;
			Mesh = Loader.GetMesh("rock.mesh");
			Rockness = 1;
		}
		
		public override void Die(GameState g) {
			while(Misc.Rand.NextDouble() < 0.3) {
				Resource rs = new Resource(Loc, 0);
				double rx = (Misc.Rand.NextDouble() - 0.5) * Vel.X;
				double ry = (Misc.Rand.NextDouble() - 0.5) * Vel.Y;
				rs.Vel = new Vector2d(rx, ry);
				g.AddObj(rs);
			}
		}
	}
	
	public class Block : BaseObj {
		public Block(Vector2d loc, double facing) : base(loc, facing) {
			Collider = new CircleCollider(loc, 10);
			Mass = 10;
			Hits = 100;
			MaxHits = Hits;
			Mesh = Loader.GetMesh("block.mesh");
			Rockness = 2;
		}
	}
	public class Cloud : BaseObj {
		public Cloud(Vector2d loc, double facing) : base(loc, facing) {
			Collider = new CircleCollider(loc, 10);
			Mass = 10;
			Hits = 10;
			MaxHits = Hits;
			Weirdness = 1;
		}
	}
	public class Fan : BaseObj {
		public double Force {get;set;}
		public Fan(Vector2d loc, double facing) : base(loc, facing) {
			Collider = new CircleCollider(loc, 10);
			Mass = 1000;
			Hits = 150;
			MaxHits = Hits;
			Mesh = Loader.GetMesh("fan.mesh");
			RVel = 0.5;
			Weirdness = 4;
			
			// Ranges from -1 to +1
			//Force = (Misc.Rand.NextDouble() * 2 - 1);
			if(Misc.Rand.NextDouble() > 0.5) {
				Force = 1;
			} else {
				// This is too much force for a player to escape with current stats.
				// Dangers should be dangerous.
				Force = -1;
			}
		}
		
		// We sort of cheat and use this function to push the nearby
		// object away from the fan.
		public override bool Colliding(IGameObj o) {
			// If the objects are too close... such as actually in contact...
			// then EXTREME things can happen.
			if(base.Colliding(o)) {
				return true;
			} else {
				Vector2d separation = o.Loc - Loc;
				double distance2 = separation.LengthSquared;
				Vector2d sepDir = Vector2d.NormalizeFast(separation);
				if(distance2 < 10000) {
					separation = Vector2d.Multiply(sepDir, ((10000 - distance2) / 100) * Force);
					o.Push(separation);
				}
				return false;
			}
		}
		
		
		public override void OnCollide(IGameObj o, GameState g) {
			o.Damage(2);
		}
	}
	// Floating energy ball
	public class Feb : BaseObj {
		public Feb(Vector2d loc, double facing) : base(loc, facing) {
			Weirdness = 3;
			Dangerness = 1;
		}
	}
	public class Bubble : BaseObj {
		public Bubble(Vector2d loc, double facing) : base(loc, facing) {
			Weirdness = 2;
		}
	}
	public class Flame : BaseObj {
		public Flame(Vector2d loc, double facing) : base(loc, facing) {
			Weirdness = 2;
		}
	}
	#endregion
	
	#region Weapons
	public class Rocket : BaseShip {
		int Time {get;set;}
		public Rocket(Vector2d loc, double facing, IController ai) : base(loc, facing, ai) {
			Mesh = Loader.GetMesh("rocket.mesh");
			Collider = new CircleCollider(loc, 3);
			Hits = 10;
			MaxHits = Hits;
			Time = 20;
			Mass = 10;
			ThrustForce = 30;
		
		}
		
		public override void Calc(GameState g) {
			base.Calc(g);
			Time -= 1;
			if(Time < 1) {
				Hits = 0;
			}
			
			if(Thrusting) {
				Mesh[] m = new Mesh[4];
				m[0] = Loader.GetBillboard("engine-spark1.bb");
				m[1] = Loader.GetBillboard("engine-spark2.bb");
				m[2] = Loader.GetBillboard("engine-spark3.bb");
				m[3] = Loader.GetBillboard("engine-spark4.bb");
				for(int i = 0; i < 6; i++) {
					int mesh = Misc.Rand.Next(m.Length);
					Particle p = new Particle(Loc, 0, m[mesh]);
					p.Vel = new Vector2d(Misc.Rand.NextDouble() * 2 + 3, Misc.Rand.NextDouble());
					p.Vel = Misc.Vector2dRotate(p.Vel, Facing + Math.PI);
					p.Hits = 15;
					g.AddParticle(p);
				}
				Thrusting = false;
			}
		}

		public override void OnCollide(IGameObj o, GameState g) {
			Hits = 0;
			o.Damage(15);
			//Console.WriteLine("Boom");
		}
		
		public override void Die(GameState g) {
			Mesh[] m = new Mesh[4];
			m[0] = Loader.GetBillboard("engine-spark1.bb");
			m[1] = Loader.GetBillboard("engine-spark2.bb");
			m[2] = Loader.GetBillboard("engine-spark3.bb");
			m[3] = Loader.GetBillboard("engine-spark4.bb");
			for(int i = 0; i < 25; i++) {
				int mesh = Misc.Rand.Next(m.Length);
				Particle p = new Particle(Loc, 0, m[mesh]);
				p.Vel = new Vector2d(Misc.Rand.NextDouble() * 2, Misc.Rand.NextDouble() * 2);
				p.Vel = Misc.Vector2dRotate(p.Vel, Facing + Math.PI);
				p.Hits = 15;
				g.AddParticle(p);
			}
		}
	}
	
	public class BaseShot : BaseObj {
		public int DamageDone { get; set; }
		public BaseShot(Vector2d loc, double facing) : base(loc, facing) {
			Mesh = Loader.GetMesh("player.mesh");
			DamageDone = 1;
			Shootable = false;
			Impactable = false;
		}
		
		public override bool Colliding(IGameObj o) {
			if(o.Shootable) {
				return base.Colliding(o);
			} else {
				return false;
			}
		}

		public override void OnCollide(IGameObj p, GameState g) {
			Hits = 0;
			p.Damage(DamageDone);
		}

		public override void Calc(GameState g) {
			//Console.WriteLine("Calcing!");
						/*
			OldLoc = Loc;
			OldFacing = Facing;
			ICollection<IGameObj> nearbyObjects = g.GetObjectsWithin(Loc, EffectRange);
			CalcPhysics(g);
			CalcCollision(g, nearbyObjects);
			Collider.Location = Loc;
			*/
			base.Calc(g);
			Hits -= 1;
			if(Hits > 0) {
			} else {
				g.KillObj(this);
			}
		}
	}
	
	public class VulcanShot : BaseShot {
		public VulcanShot(Vector2d loc, double facing) : base(loc, facing) {
			Collider = new CircleCollider(loc, 3);
			Mass = 5;
			Mesh = Loader.GetBillboard("vulcanshot.bb");
			Hits = 15;
			MaxHits = Hits;
			DamageDone = 1;
		}
	}
	
	public class PulsarShot : BaseShot {
		const int Range = 300;
		public PulsarShot(Vector2d loc, double facing) : base(loc, facing) {
			Vector2d ray = Misc.Vector2dFromDirection(facing);
			ray = Vector2d.Multiply(ray, Range);
			// Long range here!
			Collider = new RayCollider(loc, ray);
			Mesh = Loader.GetBillboard("pulsarshot.bb");
			Hits = 2;
			MaxHits = Hits;
			DamageDone = 25;
		}
		
		// Have to move the billboard so the middle of the billboard is in the middle of the ray,
		// instead of the middle of the billboard being at the start of the ray.
		
		public override void Draw(double dt) {
			Vector2d lerploc = Misc.LerpVector2d(OldLoc, Loc, dt);
			double lerpfacing = Misc.LerpDouble(OldFacing, Facing, dt);
			double offsetrange = Range / 2;
			Vector2d offsetv = Misc.Vector2dFromDirection(lerpfacing);
			offsetv = Vector2d.Multiply(offsetv, offsetrange);
			Vector2d loc = Vector2d.Add(lerploc, offsetv);
			Vector3d v = new Vector3d(loc);
			Quaterniond q = Quaterniond.FromAxisAngle(Graphics.OutOfScreen, lerpfacing);
			Mesh.Draw(v, q);
		}
	}
	
	public class PhantomHammerShot : BaseShot {
		const int Range = 350;
		public PhantomHammerShot(Vector2d loc, double facing) : base(loc, facing) {
			Vector2d ray = Misc.Vector2dFromDirection(facing);
			ray = Vector2d.Multiply(ray, Range);
			// Long range here!
			Collider = new RayCollider(loc, ray);
			Mesh = Loader.GetBillboard("phantomhammershot.bb");
			Hits = 3;
			DamageDone = 200;
		}

		// Have to move the billboard so the middle of the billboard is in the middle of the ray,
		// instead of the middle of the billboard being at the start of the ray.

		public override void Draw(double dt) {
			Vector2d lerploc = Misc.LerpVector2d(OldLoc, Loc, dt);
			double lerpfacing = Misc.LerpDouble(OldFacing, Facing, dt);
			double offsetrange = Range / 2;
			Vector2d offsetv = Misc.Vector2dFromDirection(lerpfacing);
			offsetv = Vector2d.Multiply(offsetv, offsetrange);
			Vector2d loc = Vector2d.Add(lerploc, offsetv);
			Vector3d v = new Vector3d(loc);
			Quaterniond q = Quaterniond.FromAxisAngle(Graphics.OutOfScreen, lerpfacing);
			Mesh.Draw(v, q);
		}
	}
	
	public class ShootgunShot : BaseShot {
		public ShootgunShot(Vector2d loc, double facing) : base(loc, facing) {
			Collider = new CircleCollider(loc, 10);
			Mass = 10;
			Mesh = Loader.GetBillboard("shootshot.bb");
			DamageDone = 1;
			Hits = 5;
		}
	}
	
	public class RailgunShot : BaseShot {
		public RailgunShot(Vector2d loc, double facing) : base(loc, facing) {
			Collider = new CircleCollider(loc, 10);
			Mass = 10;
			Mesh = Loader.GetBillboard("railgunshot.bb");
			DamageDone = 40;
			Hits = 30;
		}
		
		// Kershove!
		public override void OnCollide(IGameObj o, GameState g) {
			Vector2d force = Vector2d.NormalizeFast(Vel);
			force = Vector2d.Multiply(force, 300);
			o.Push(force);
			base.OnCollide(o, g);
		}
	}
	
	public class FireballShot : BaseShot {
		public FireballShot(Vector2d loc, double facing) : base(loc, facing) {
			Collider = new CircleCollider(loc, 10);
			Mass = 10;
			Mesh = Loader.GetBillboard("fireballshot.bb");
			DamageDone = 25;
			Hits = MaxHits = 15;
		}
	}
	
	public class GrenadeShot : BaseShot {
		// Because of the large AOE, which is represented
		// as a large hit box, these distinctly require a 
		// safe time before which they collide with nothing...
		int LifeTime;
		public GrenadeShot(Vector2d loc, double facing) : base(loc, facing) {
			Hits = 11;
			MaxHits = Hits;
			Collider = new CircleCollider(loc, 15);
			Mass = 10;
			RVel = Misc.Rand.NextDouble() * 4;
			Mesh = Loader.GetMesh("grenade.mesh");
			DamageDone = 5;
			LifeTime = 0;
			Shootable = true;
		}
		
		public override bool Colliding(IGameObj o) {
			if(LifeTime < 5) {
				return false;
			} else {
				return base.Colliding(o);
			}
		}
		
		public override void Calc(GameState g) {
			LifeTime += 1;
			base.Calc(g);
		}
		
		public override void Die(GameState g) {
			Mesh[] m = new Mesh[4];
			m[0] = Loader.GetBillboard("engine-spark1.bb");
			m[1] = Loader.GetBillboard("engine-spark2.bb");
			m[2] = Loader.GetBillboard("engine-spark3.bb");
			m[3] = Loader.GetBillboard("engine-spark4.bb");
			for(int i = 0; i < 25; i++) {
				int mesh = Misc.Rand.Next(m.Length);
				Particle p = new Particle(Loc, 0, m[mesh]);
				p.Vel = new Vector2d(Misc.Rand.NextDouble() * 2, Misc.Rand.NextDouble() * 2);
				p.Vel = Misc.Vector2dRotate(p.Vel, Facing + Math.PI);
				p.Hits = 9;
				g.AddParticle(p);
			}
		}
	}
	#endregion
	
	#region Ships
	public class Player : BaseShip {
		public Player(Vector2d vec, double facing, IController c) : base(vec, facing, c) {
			Mesh = Loader.GetMesh("player.mesh");
			
			Moment = 200.0;
			Mass = 1000.0;
			Vulnerable = true;
			ThrustForce = 500.0;
			DragAmount = 0.9;
			TurnForce = 10.0;
			RDragAmount = 0.8;
			BrakeDrag = 0.90;
			
			Hits = MaxHits = 100;
			MaxEnergy = Energy = 100;
			EnergyRegen = 1;
			
			Vector2d[] firepoints = new Vector2d[2];
			firepoints[0].X = 23;
			firepoints[0].Y = 5;
			firepoints[1].X = 23;
			firepoints[1].Y = -5;
			Vector2d[] firedirs = new Vector2d[2];
			firedirs[0].X = 1;
			firedirs[0].Y = 0;
			firedirs[1].X = 1;
			firedirs[1].Y = 0;
			WeaponPoint[] weaponpoints1 = new WeaponPoint[2];
			WeaponPoint[] weaponpoints2 = new WeaponPoint[2];
			WeaponPoint[] weaponpoints3 = new WeaponPoint[2];
			weaponpoints1[0] = WeaponFactory.VulcanPoint(firepoints[0], firedirs[0]);
			weaponpoints1[1] = WeaponFactory.VulcanPoint(firepoints[1], firedirs[1]);
			weaponpoints2[0] = WeaponFactory.GrenadePoint(firepoints[0], firedirs[0]);
			weaponpoints2[1] = WeaponFactory.GrenadePoint(firepoints[1], firedirs[1]);
			weaponpoints3[0] = WeaponFactory.PulsarPoint(firepoints[0], firedirs[0]);
			weaponpoints3[1] = WeaponFactory.PulsarPoint(firepoints[1], firedirs[1]);

			Weapons = new BaseWeapon[3];
			Weapons[0] = new BaseWeapon(1, 3, weaponpoints1, ShotMode.CYCLE);
			Weapons[1] = new BaseWeapon(5, 15, weaponpoints2, ShotMode.BURST);
			Weapons[2] = new BaseWeapon(15, 20, weaponpoints3, ShotMode.BURST);
		}
		public override void Calc(GameState g) {
			base.Calc(g);
		}
		
		public override void OnCollide(IGameObj o, GameState g) {
			//Console.WriteLine("Player hit something...");
		}
	}
	
	public class Turret : BaseShip {
		public Turret(Vector2d vec, double facing, IController c) : base(vec, facing, c) {
			Mesh = Loader.GetMesh("turret.mesh");
			Dangerness = 5;
			
			Moment = 200.0;
			Mass = 1000.0;
			Vulnerable = true;
			ThrustForce = 0.0;
			DragAmount = 0.9;
			TurnForce = 10.0;
			RDragAmount = 0.8;
			BrakeDrag = 0.90;
			
			Hits = MaxHits = 20;
			MaxEnergy = Energy = 100;
			EnergyRegen = 1;
			
			Vector2d[] firepoints = new Vector2d[3];
			firepoints[0].X = 20;
			firepoints[0].Y = 1;
			Vector2d[] firedirs = new Vector2d[3];
			firedirs[0].X = 1;
			firedirs[0].Y = 0.00;
			
			WeaponPoint[] weaponpoints = new WeaponPoint[3];
			weaponpoints[0] = WeaponFactory.VulcanPoint(firepoints[0], firedirs[0]);
			Weapons = new BaseWeapon[1];
			Weapons[0] = new BaseWeapon(2, 1, weaponpoints, ShotMode.BURST);
		}
	}
	
	public class DeathTurret : BaseShip {
		public DeathTurret(Vector2d loc, double facing, IController c) : base(loc, facing, c) {
			Mesh = Loader.GetMesh("deathturret.mesh");
			Dangerness = 10;
			
			Moment = 100.0;
			Mass = 2000.0;
			Vulnerable = true;
			ThrustForce = 0.0;
			DragAmount = 0.9;
			TurnForce = 1.0;
			RDragAmount = 0.8;
			BrakeDrag = 0.90;
			
			Hits = MaxHits = 60;
			MaxEnergy = Energy = 100;
			EnergyRegen = 1;
			
			
			
			Vector2d[] firepoints = new Vector2d[3];
			firepoints[0].X = 20;
			firepoints[0].Y = 10;
			firepoints[1].X = 20;
			firepoints[1].Y = -10;
			Vector2d[] firedirs = new Vector2d[3];
			firedirs[0].X = 1;
			firedirs[0].Y = 0.05;
			firedirs[1].X = 1;
			firedirs[1].Y = -0.05;
			
			WeaponPoint[] weaponpoints = new WeaponPoint[3];
			weaponpoints[0] = WeaponFactory.VulcanPoint(firepoints[0], firedirs[0]);
			weaponpoints[1] = WeaponFactory.VulcanPoint(firepoints[1], firedirs[1]);
			Weapons = new BaseWeapon[1];
			Weapons[0] = new BaseWeapon(1, 10, weaponpoints, ShotMode.CYCLE);
		}
	}
	
	public class Scout : BaseShip {
		public Scout(Vector2d loc, double facing, IController ai) : base(loc, facing, ai) {
			Mesh = Loader.GetMesh("scout.mesh");
			
			Moment = 100.0;
			Mass = 200.0;
			Vulnerable = true;
			ThrustForce = 75.0;
			DragAmount = 0.9;
			TurnForce = 10.0;
			RDragAmount = 0.85;
			BrakeDrag = 0.90;
			
			Hits = MaxHits = 15;
			MaxEnergy = Energy = 30;
			EnergyRegen = 1;
			
			
			Vector2d[] firepoints = new Vector2d[1];
			firepoints[0].X = 25;
			firepoints[0].Y = 0;
			Vector2d[] firedirs = new Vector2d[1];
			firedirs[0].X = 1;
			firedirs[0].Y = 0;
			WeaponPoint[] weaponpoints = new WeaponPoint[1];
			weaponpoints[0] = WeaponFactory.ShootgunPoint(firepoints[0], firedirs[0]);
			Weapons = new BaseWeapon[1];
			Weapons[0] = new BaseWeapon(20, 10, weaponpoints, ShotMode.BURST);
		}
	}
	
	public class Swarmer : BaseShip {
		public Swarmer(Vector2d loc, double facing, IController ai) : base(loc, facing, ai) {
			Mesh = Loader.GetMesh("swarmer.mesh");
			Mass = 150;
			ThrustForce = 3;
			DragAmount = 0.9;
			TurnForce = 0.1;
			RDragAmount = 0.00;
			MaxHits = Hits = 10;
			
			Moment = 50.0;
			Mass = 150.0;
			Vulnerable = true;
			ThrustForce = 75.0;
			DragAmount = 0.9;
			TurnForce = 10.0;
			RDragAmount = 0.85;
			BrakeDrag = 0.90;
			
			Hits = MaxHits = 10;
			MaxEnergy = Energy = 20;
			EnergyRegen = 1;
			
			Vector2d[] firepoints = new Vector2d[1];
			firepoints[0].X = 25;
			firepoints[0].Y = 0;
			Vector2d[] firedirs = new Vector2d[1];
			firedirs[0].X = 1;
			firedirs[0].Y = 0;
			WeaponPoint[] weaponpoints = new WeaponPoint[1];
			weaponpoints[0] = WeaponFactory.ShootgunPoint(firepoints[0], firedirs[0]);
			Weapons = new BaseWeapon[1];
			Weapons[0] = new BaseWeapon(20, 10, weaponpoints, ShotMode.BURST);
		}
	}
	
	public class Fighter : BaseShip {
		public Fighter(Vector2d loc, double facing, IController ai) : base(loc, facing, ai) {
			Mesh = Loader.GetMesh("fighter.mesh");
			
			Moment = 200.0;
			Mass = 500.0;
			Vulnerable = true;
			ThrustForce = 200.0;
			DragAmount = 0.9;
			TurnForce = 10.0;
			RDragAmount = 0.8;
			BrakeDrag = 0.90;
			
			Hits = MaxHits = 50;
			MaxEnergy = Energy = 100;
			EnergyRegen = 1;
			
			
			Vector2d[] firepoints = new Vector2d[1];
			firepoints[0].X = 25;
			firepoints[0].Y = 0;
			Vector2d[] firedirs = new Vector2d[1];
			firedirs[0].X = 1;
			firedirs[0].Y = 0;
			WeaponPoint[] weaponpoints = new WeaponPoint[1];
			weaponpoints[0] = WeaponFactory.VulcanPoint(firepoints[0], firedirs[0]);
			Weapons = new BaseWeapon[1];
			Weapons[0] = new BaseWeapon(2, 2, weaponpoints, ShotMode.BURST);
		}
	}
	public class Gunboat : BaseShip {
		public Gunboat(Vector2d loc, double facing, IController ai) : base(loc, facing, ai) {
			Mesh = Loader.GetMesh("gunboat.mesh");
			
			Moment = 1000.0;
			Mass = 2000.0;
			Vulnerable = true;
			ThrustForce = 300.0;
			DragAmount = 0.6;
			TurnForce = 5.0;
			RDragAmount = 0.85;
			BrakeDrag = 0.90;
			
			Hits = MaxHits = 150;
			MaxEnergy = Energy = 130;
			EnergyRegen = 3;
			
			Vector2d[] firepoints = new Vector2d[1];
			firepoints[0].X = 25;
			firepoints[0].Y = 0;
			Vector2d[] firedirs = new Vector2d[1];
			firedirs[0].X = 1;
			firedirs[0].Y = 0;
			WeaponPoint[] weaponpoints = new WeaponPoint[1];
			weaponpoints[0] = WeaponFactory.RailgunPoint(firepoints[0], firedirs[0]);
			Weapons = new BaseWeapon[1];
			Weapons[0] = new BaseWeapon(20, 10, weaponpoints, ShotMode.BURST);
		}
	}
	public class Cruiser : BaseShip {
		public Cruiser(Vector2d loc, double facing, IController ai) : base(loc, facing, ai) {
			Mesh = Loader.GetMesh("cruiser.mesh");
			Mass = 10000;
			ThrustForce = 50;
			DragAmount = 0.6;
			TurnForce = 0.01;
			RDragAmount = 0.90;
			MaxHits = Hits = 1000;
			
			Vector2d[] firepoints = new Vector2d[4];
			firepoints[0].X = 25;
			firepoints[0].Y = 1;
			firepoints[1].X = 25;
			firepoints[1].Y = -1;
			firepoints[2].X = 25;
			firepoints[2].Y = 2;
			firepoints[3].X = 25;
			firepoints[3].Y = -2;
			
			Vector2d[] firedirs = new Vector2d[1];
			firedirs[0].X = 1;
			firedirs[0].Y = 0;
			WeaponPoint[] weaponpoints = new WeaponPoint[4];
			weaponpoints[0] = WeaponFactory.RailgunPoint(firepoints[0], firedirs[0]);
			weaponpoints[1] = WeaponFactory.RailgunPoint(firepoints[1], firedirs[0]);
			weaponpoints[2] = WeaponFactory.RailgunPoint(firepoints[2], firedirs[0]);
			weaponpoints[3] = WeaponFactory.RailgunPoint(firepoints[3], firedirs[0]);
			Weapons = new BaseWeapon[1];
			Weapons[0] = new BaseWeapon(20, 10, weaponpoints, ShotMode.BURST);
		}
	}
	public class Carrier : BaseShip {
		public Carrier(Vector2d loc, double facing, IController ai) : base(loc, facing, ai) {
			Mesh = Loader.GetMesh("carrier.mesh");
			Mass = 10000;
			ThrustForce = 50;
			DragAmount = 0.6;
			TurnForce = 0.01;
			RDragAmount = 0.90;
			MaxHits = Hits = 800;
			
			Vector2d[] firepoints = new Vector2d[2];
			firepoints[0].X = 25;
			firepoints[0].Y = 1;
			firepoints[1].X = 25;
			firepoints[1].Y = -1;
			Vector2d[] firedirs = new Vector2d[1];
			firedirs[0].X = 1;
			firedirs[0].Y = 0;
			WeaponPoint[] weaponpoints = new WeaponPoint[2];
			weaponpoints[0] = WeaponFactory.VulcanPoint(firepoints[0], firedirs[0]);
			weaponpoints[1] = WeaponFactory.VulcanPoint(firepoints[1], firedirs[0]);
			Weapons = new BaseWeapon[1];
			Weapons[0] = new BaseWeapon(20, 10, weaponpoints, ShotMode.BURST);
		}
	}
	public class Dreadnought : BaseShip {
		public Dreadnought(Vector2d loc, double facing, IController ai) : base(loc, facing, ai) {
			Mesh = Loader.GetMesh("dreadnought.mesh");
			Mass = 20000;
			ThrustForce = 50;
			DragAmount = 0.4;
			TurnForce = 0.004;
			RDragAmount = 0.95;
			MaxHits = Hits = 3500;
			
			Vector2d[] firepoints = new Vector2d[1];
			firepoints[0].X = 25;
			firepoints[0].Y = 0;
			Vector2d[] firedirs = new Vector2d[1];
			firedirs[0].X = 1;
			firedirs[0].Y = 0;
			WeaponPoint[] weaponpoints = new WeaponPoint[1];
			weaponpoints[0] = WeaponFactory.PhantomHammerPoint(firepoints[0], firedirs[0]);
			Weapons = new BaseWeapon[1];
			Weapons[0] = new BaseWeapon(50, 10, weaponpoints, ShotMode.BURST);
		}
	}
	public class Feral : BaseShip {
		public Feral(Vector2d loc, double facing, IController ai) : base(loc, facing, ai) {
			Mesh = Loader.GetMesh("feral.mesh");
			Vector2d[] firepoints = new Vector2d[1];
			firepoints[0].X = 25;
			firepoints[0].Y = 0;
			Vector2d[] firedirs = new Vector2d[1];
			firedirs[0].X = 1;
			firedirs[0].Y = 0;
			WeaponPoint[] weaponpoints = new WeaponPoint[1];
			weaponpoints[0] = WeaponFactory.PulseBeamPoint(firepoints[0], firedirs[0]);
			Weapons = new BaseWeapon[1];
			Weapons[0] = new BaseWeapon(20, 10, weaponpoints, ShotMode.BURST);
		}
	}
	public class Berserker : BaseShip {
		public Berserker(Vector2d loc, double facing, IController ai) : base(loc, facing, ai) {
			Mesh = Loader.GetMesh("berserker.mesh");
			
			Vector2d[] firepoints = new Vector2d[2];
			firepoints[0].X = 25;
			firepoints[0].Y = 1;
			firepoints[1].X = 25;
			firepoints[1].Y = -1;
			Vector2d[] firedirs = new Vector2d[2];
			firedirs[0].X = 1;
			firedirs[0].Y = 0;
			WeaponPoint[] weaponpoints = new WeaponPoint[2];
			weaponpoints[0] = WeaponFactory.PulseBeamPoint(firepoints[0], firedirs[0]);
			weaponpoints[0] = WeaponFactory.PulseBeamPoint(firepoints[1], firedirs[0]);
			Weapons = new BaseWeapon[1];
			Weapons[0] = new BaseWeapon(20, 10, weaponpoints, ShotMode.BURST);
		}
	}
	public class Spider : BaseShip {
		public Spider(Vector2d loc, double facing, IController ai) : base(loc, facing, ai) {
			Mesh = Loader.GetMesh("spider.mesh");
			
			Vector2d[] firepoints = new Vector2d[2];
			firepoints[0].X = 25;
			firepoints[0].Y = 1;
			firepoints[0].X = 25;
			firepoints[0].Y = -1;
			Vector2d[] firedirs = new Vector2d[1];
			firedirs[0].X = 1;
			firedirs[0].Y = 0;
			WeaponPoint[] weaponpoints = new WeaponPoint[1];
			weaponpoints[0] = WeaponFactory.FireballPoint(firepoints[0], firedirs[0]);
			weaponpoints[0] = WeaponFactory.FireballPoint(firepoints[1], firedirs[0]);
			Weapons = new BaseWeapon[1];
			Weapons[0] = new BaseWeapon(10, 10, weaponpoints, ShotMode.BURST);
		}
	}
	public class Nomad : BaseShip {
		public Nomad(Vector2d loc, double facing, IController ai) : base(loc, facing, ai) {
			Mesh = Loader.GetMesh("nomad.mesh");
			
			Vector2d[] firepoints = new Vector2d[1];
			firepoints[0].X = 25;
			firepoints[0].Y = 0;
			Vector2d[] firedirs = new Vector2d[1];
			firedirs[0].X = 1;
			firedirs[0].Y = 0;
			WeaponPoint[] weaponpoints = new WeaponPoint[1];
			weaponpoints[0] = WeaponFactory.VulcanPoint(firepoints[0], firedirs[0]);
			Weapons = new BaseWeapon[1];
			Weapons[0] = new BaseWeapon(2, 10, weaponpoints, ShotMode.BURST);
		}
	}
	
	public class Bird : BaseShip {
		public Bird(Vector2d loc, double facing, IController ai) : base(loc, facing, ai) {
			Mesh = Loader.GetMesh("bird.mesh");
			Collider = new Collider();
			
			Moment = 200.0;
			Mass = 200.0;
			Vulnerable = true;
			ThrustForce = 75.0;
			DragAmount = 0.9;
			TurnForce = 10.0;
			RDragAmount = 0.85;
			BrakeDrag = 0.90;
			
			Hits = MaxHits = 15;
			MaxEnergy = Energy = 30;
			EnergyRegen = 1;
			EffectRange = 500;
		
		}
		
		// Birds are harmless, of course!
		public override void Fire(GameState g) {
			
		}
	}
	
	/*
	public class FinalBoss : BaseShip {
	}
	
	 */
	#endregion
	
}
	