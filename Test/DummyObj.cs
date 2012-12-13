using System;

using OpenTK;
using Lost;

namespace Lost.Test {
	// Basically a BaseObj with no drawing, so I don't have to init OpenGL and load resources and such just to
	// do physics to it.
	public class DummyObj : BaseObj {
		// XXX: Broked since we MUST call a base constructor, which tries to load a mesh, which
		// doesn't work since we haven't initialized OpenGL.
		public DummyObj(Vector2d loc, double facing) : base(loc, facing) {			
			Loc = loc;
			OldLoc = loc;
			Facing = facing;
			OldFacing = facing;
			Id = Misc.GetID();
			Collider = new CircleCollider(loc, 10);
			
			Mesh = null;
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

		public override void Draw(double dt) {
			return;
		}
	}

}

