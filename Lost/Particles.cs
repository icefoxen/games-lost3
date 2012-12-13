using System;
using OpenTK;

// Hmmm, this will probably load properties from an XML file....
// Particle properties: Life, billboard, transparency, movement pattern, 
// Generator properties: Pattern (direction, angle), frequency, number, particle list...
// Shapes: Cone, circle...

namespace Lost {

	// Moves in some way, lasts a certain time, draws something, doesn't interact.
	// We might also want to make these not inherit from BaseObj, to save memory.
	// Could also make a freelist, but eh.
	// But for now, oh well.
	public class Particle : BaseObj {
		public Particle(Vector2d vec, double facing) : this(vec, facing, "blue-spark.bb") {
		}
		
		public Particle(Vector2d vec, double facing, string bb) : this(vec, facing, Loader.GetBillboard(bb)) {
			
		}
		
		public Particle(Vector2d vec, double facing, Mesh bb) : base(vec, facing) {
			Mesh = bb;
			Mass = 0;
			Moment = 0;
			Collider = new Collider();
			Hits = 100;
		}
		
		public override void Push(Vector2d f) {}
		public override void Rotate(double f) {}
		public override void Drag(double f) {}
		public override void RDrag(double f) {}
		
		public override bool Colliding(IGameObj g) {
			return false;
		}
		
		public override void Calc(GameState g) {
			OldLoc = Loc;
			OldFacing = Facing;
			Damage(1);
			CalcPhysics(g);
			if(Hits > 0) {
				//g.AddParticle(this);
			} else {
				g.KillParticle(this);
			}
		}
		
		public override void Die(GameState g) {}
	}
	
	public delegate IGameObj ParticleMaker(Vector2d loc, double facing);
	public class ParticleFactory {
		public static IGameObj MakeParticle(Vector2d loc, double facing) {
			return new Particle(loc, facing);
		}
		
		// C-c-c-COMBINATORS!
		// ...okay, figure out what combinators we want.
		// OneOf, ManyOf, combinators that emit particles in a particular (hah) direction
		// and velocity and rotation...
		// Hmm.
		// XXX: You know, I think I'll just keep particle emission hard-wired for now...
		// XXX: You know, particles look a lot better when they're animated!
		// We want to flip between textures, interpolate between textures, change
		// object size, rotate and move textures/objects...
		public static IGameObj OneOf(ParticleMaker[] m, Vector2d loc, double f) {
			int choice = Misc.Rand.Next(m.Length);
			return m[choice](loc, f);
		}
	}

	
	// Creates more particles.
	// Types: Circular, directional, conical, constant/burst...
	public class ParticleGenerator : Particle {
		protected Func<Particle> PartMaker;
		protected int Last; // Time until next emit
		protected int Freq; // How often between emits
		protected int Count; // How many particles to emit
		public ParticleGenerator(Vector2d loc, double facing) : base(loc, facing) {}
		public override void Calc(GameState g) {
			base.Calc(g);
			if(Last == 0) {
				Last = Freq;
				Emit(g);
			} else {
				Last -= 1;
			}
		}
		public virtual void Emit(GameState g) {
			
		}
	}
	
	public class CircleGenerator : ParticleGenerator {
		double Speed;
		public CircleGenerator(Vector2d loc, double speed, int count, int frequency, Func<Particle> f) 
		: base(loc, 0) {
			Speed = speed;
			PartMaker = f;
			Count = count;
			Freq = frequency;
			Mesh = Loader.GetBillboard("shot.bb");
		}
		
		public override void Emit(GameState g) {
			for(int i = 0; i < Count; i++) {
				Particle p = PartMaker();
				p.Loc = Loc;
				double angle = Misc.Rand.NextDouble() * (Math.PI*2);
				Vector2d vel;
				vel.X = Speed*Math.Cos(angle);
				vel.Y = Speed*Math.Sin(angle);
				p.Vel = vel;
				g.AddParticle(p);
			}
			Console.WriteLine("Emitted");
		}
	}
	
	public class ConeGenerator : ParticleGenerator {
		double Angle;
		double Speed;
		public ConeGenerator(Vector2d loc, double facing, double angle, double speed, 
			int count, int frequency, Func<Particle> f) : base(loc, facing) {
			PartMaker = f;
			Angle = angle;
			Count = count;
			Speed = speed;
			Freq = frequency;
		}
		
		public override void Emit(GameState g) {
			for(int i = 0; i < Count; i++) {
				Particle p = PartMaker();
				p.Loc = Loc;
				// Oh gods is this math right?
				double angle = ((Misc.Rand.NextDouble()-0.5) * Angle) + Facing;
				Vector2d vel;
				vel.X = Speed*Math.Cos(angle);
				vel.Y = Speed*Math.Sin(angle);
				p.Vel = vel;
				g.AddParticle(p);
			}
		}
	}
}
