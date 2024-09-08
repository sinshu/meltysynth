using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MeltySynth;

public class Game1 : Game
{
    private GraphicsDeviceManager graphics;

    private MidiFile? midiFile;
    private MidiPlayer? midiPlayer;

    public Game1()
    {
        graphics = new GraphicsDeviceManager(this);
    }

    protected override void LoadContent()
    {
        midiFile = new MidiFile(@"C:\Windows\Media\flourish.mid");
        midiPlayer = new MidiPlayer("TimGM6mb.sf2");
    }

    protected override void UnloadContent()
    {
        if (midiPlayer != null)
        {
            midiPlayer.Dispose();
            midiPlayer = null;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if (midiPlayer!.State == SoundState.Stopped)
        {
            midiPlayer.Play(midiFile!, true);
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        base.Draw(gameTime);
    }
}
