vs-trim-line-ends-on-save-plugin
================================

Visual Studio plugin to trim line ends on save. Retains cursor and scroll position.

Allows also to enforce line ending format via settings page:
![missing image](http://idct.pl/img/trimtosave.png)

Options:

* `Windows` - \r\n
* `Unix` - \r\n
* `VisualStudio` - uses the Environment.NewLine
* `Current` - keeps line endings encountered in the file

By default uses the `VisualStudio` option.


Usage
=====

Install the .VSIX plugin which can be found in the `.\binary\Release\` folder

TODO
====

* Interface to limit file types
* Option to enable/disable the plugin via menu interface
* memory optimisation
