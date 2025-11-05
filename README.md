## Due VGA Font Converter
### Purpose
Due VGA is an adoptation of a custom made "video card" on top of the Arduino Due development board.
The firmware allows us to use an Arduino Due to display output to a VGA monitor and supports PS2 Keyboard and Mouse input.

Fonts for the "OS" can be converted from TTF font files that are readily available. This utility lets us do that!

To use, run from the command line passing a few parameters to achieve your desired font

Example:
`./FontConverter -font ./Fonts/Ubuntu-R.ttf -size 18 -header Font.h -bmp 1`
`font` parameter will specify the input TTF file to use
`size` parameter will determine the font size of the generated font
`header` parameter will specify the file name of the generated header file
`bmp` parameter will specify if bitmap files of each character should be generated._Default is off_
