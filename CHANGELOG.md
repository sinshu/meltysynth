# v2.2.3

- Improved voice prioritization.
- Improved fallback when no corresponding preset was found.
- Now unknown generators are ignored.

# v2.2.2

- Added fallback when no corresponding preset was found.
- Fixed wrong MIDI message handling.
- Improved error check to allow some non-standard SoundFonts.

# v2.2.1

- Added the EndOfSequence property to MidiFileSequencer.
- Added the Position property to MidiFileSequencer.
- Some minor improvements.

# v2.2.0

- This version supports .NET Standard 2.1.

# v2.1.0

- Added support for MIDI file loop extension.
- Added utility methods to convert sample format.
- Fixed pop noise in certain instruments.
- Improved performance by using Vector<T>.

# v2.0.0

- Revised the API to simplify the MIDI playback functionality.

# v1.1.3

- Fixed pop noise in certain instruments.

# v1.1.2

- Now the package contains the XML documentation.

# v1.1.1

- Revised accessibility of members.

# v1.1.0

- Implemented reverb and chorus.

# v1.0.3

- Improved the loudness correction.

# v1.0.2

- Fixed crash where the MIDI file sequencer tries to read a MIDI file even if the file is not yet loaded.

# v1.0.1

- Fixed crash due to negative sustain level.

# v1.0.0

- Most of the basic functionalities are implemented.
