using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MeltySynth;

namespace MonoGameExample
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;

        private MidiPlayer midiPlayer;
        private MidiFile midiFile;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void LoadContent()
        {
            midiPlayer = new MidiPlayer("TimGM6mb.sf2");
            midiFile = new MidiFile(@"C:\Windows\Media\flourish.mid");
        }

        protected override void UnloadContent()
        {
            midiPlayer.Dispose();
        }

        protected override void Update(GameTime gameTime)
        {
            if (midiPlayer.State == SoundState.Stopped)
            {
                midiPlayer.Play(midiFile, true);
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            base.Draw(gameTime);
        }
    }
}
