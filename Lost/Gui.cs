using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Lost {
	public class Gui {
		Mesh Template;
		Mesh HelpScreen;
		Mesh HealthBar;
		Mesh EnergyBar;
		Mesh ResourceBar;
		bool HelpScreenOn;
		
		public Gui() {
			Template = Loader.GetBillboard("gui.bb");
			HealthBar = Loader.GetBillboard("red-bar.bb");
			EnergyBar = Loader.GetBillboard("green-bar.bb");
			ResourceBar = Loader.GetBillboard("blue-bar.bb");
			HelpScreen = Loader.GetBillboard("helpscreen.bb");
			HelpScreenOn = false;
		}
		
		public void Draw(GameState g) {
			//int playerHits = g.Player.Hits;
			Vector3d target = new Vector3d();
			
			target.X = Graphics.CameraTarget.Z;
			target.Y = Graphics.CameraTarget.X;
			target.Z = Graphics.CameraDistance;
			Graphics.StartGui();
			
			double healthOffset = 36 - 8 * ((double)g.Player.Hits / (double)g.Player.MaxHits);
			double energyOffset = 36 - 8 * (g.Player.Energy / g.Player.MaxEnergy);
			
			HealthBar.Draw(Vector3d.Subtract(target, new Vector3d(47.5, healthOffset, 50)));
			EnergyBar.Draw(Vector3d.Subtract(target, new Vector3d(45, energyOffset, 50)));
			ResourceBar.Draw(Vector3d.Subtract(target, new Vector3d(41.5, 28, 50)));
			Template.Draw(Vector3d.Subtract(target, new Vector3d(0, 7.5, 50)));
			if(HelpScreenOn) {
				HelpScreen.Draw(Vector3d.Subtract(target, new Vector3d(0, 0, 50)));
			}
			
			Graphics.EndGui();
		}
		
		public void ToggleHelp() {
			HelpScreenOn = !HelpScreenOn;
		}
	}
}

