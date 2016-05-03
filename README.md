# Ocarina Text Editor
A text editor for The Legend of Zelda: Ocarina of Time. This editor currently only works on the Master Quest Debug version. Other versions may be supported in the future.


![Program image](http://i.imgur.com/mdevXFf.png)

## Features

### Searching

Messages from *Ocarina of Time* can be searched by one of two methods: text search and message ID search.

#### Text Search

Typing the desired search term into the search box will filter the messages in the message panel so that only those that contain the term are displayed.
For example, typing in "Navi" will display only the messages that contain the string "Navi". This is case sensitive, so "navi" and "Navi" will not return the
same results.

#### Message ID Search

*Ocarina of Time* gives each message a unique ID for use in the game. Typing in the string "msgid:" followed by an ID will display the message with that ID, if it exists.
For example, msgid:20 will search for a message with an ID of 20. The ID must be in decimal format, and *not* hexadecimal.

### Control Tags

*Ocarina of Time* uses control codes to modify the text as it draws, such as changing the color or inserting special characters. In the editor, most of these
are represented by text between < and > chevrons. Detailed information about control tags and how they are used in the editor can be found [here](https://github.com/Sage-of-Mirrors/Ocarina-Text-Editor/wiki/Control-Tags).

## Textbox Types

*Ocarina of Time* has several backgrounds on which it can draw text. The available types are described here.

### Black
![Black](http://i.imgur.com/4aWwwDo.jpg)

This type displays the text over a translucent black background. It is the most commonly encountered box type, and it is mostly used for dialog.

### Wood
![Wood](http://i.imgur.com/DEqvkwa.jpg)

This type displays the text over a wooden texture. It is mainly used for signs.

### Blue
![Blue](http://i.imgur.com/G86tVec.jpg)

This type displays the text over a translucent blue background. It is mainly used for the "Item Get!" messages.

### Ocarina
![Ocarina](http://i.imgur.com/6FCYSn1.jpg)

This type is used for when Link plays an Ocarina song, and displays a musical staff.

### None (White)
![NoneWhite](http://i.imgur.com/fqGdC72.jpg)

This type displays text without a textbox. Its font color defaults to white.

### None (Black)
![NoneBlack](http://i.imgur.com/to3SwkK.jpg)

This type displays text without a textbox. Its font color defaults to Black.

### Others

In the editor, there are other textbox types after None (Black). These are thought to be dummy types that do not do anything. They are included in the editor just in case they have some
use that is currently unknown.

## Textbox Positions

There are four pre-defined positions on the screen that a textbox can be drawn at. These are described below.

### Top 1
![top1](http://i.imgur.com/kDsOOya.jpg)

This position is still not fully understood, but it may change the position of the textbox based on the position of the camera in relation to the coordinates of the speaking object.
For example, the position of the camera relative to the sign, looking slightly up at it, causes the game to create the textbox at the bottom of the screen.

### Top
![top](http://i.imgur.com/3hdY3k4.jpg)

This displays the box on the top of the screen.

### Center
![center](http://i.imgur.com/KAfRfsM.jpg)

This displays the box in the center of the screen.

### Bottom
![bottom](http://i.imgur.com/0sXt3Yi.jpg)

This displays the textbox at the bottom of the screen.