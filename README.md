# EasyAutoClicker

Welcome to EasyAutoClicker, a simple and efficient auto-clicking tool designed for ease of use and flexibility. This project is built with C# and WinUI 3.

I built this solely because I could't find a simple auto-clicker that met my needs (looping recordings). I hope you find it useful too!

I'm not a professional designer but the UI works.

## Main Page

Here you will find all of the normal auto-clicking features, such as:

- Start/Stop buttons with F9 as the hotkey
- Click Interval settings
	- Set interval in milliseconds
	- Randomize interval in milliseconds
- Click Count settings
	- Choose between mouse buttons
	- Choose between a single or double click
	- Choose how many times to perform the click
- Cursor Options
	- Current location
	- Choose a set position (coordinates)
- Start and Stop Buttons
	- Start/Stop auto-clicking with F9 hotkey
- Record & Playback button
	- Directs to the Recording page

## Recording Page

Here you can record and playback mouse clicks and keyboard presses, with the following features:

- Input type selections
- Start Recording button
	- Starts recording mouse and keyboard inputs
	- Turns into Stop Recording button
		- Prompts to save the recording as a JSON file
- Play Recording button
	- Plays back the recorded inputs
- Plyback Settings
	- Adjust the speed of playback
	- Adjust how many times to repeat the playback
	- Add some randomization to the playback speed (+ 0-Xms)
	- Load file button
		- Prompts to select a JSON file to load
- Back button
	- Returns to the main auto-clicking page