# Genealogy Tree inside Git

Allows to generate script that converts genealogy trees in gedcom format to
the list of git commands. Moreover it's possible to generate social graphs that
includes not only birth events, but relationship events such as marriage,
divorce, graduation, etc.

## Using

Run .NET Core console app `GenealogyTreeInGit.dll` with following parameters:

* First parameter - path to `.gedcom`. file;
* `--only-birth-events` - if defined ignore all events except of birth ones;
* `-r` or `--run` - run script and generate repository after script generation;
* String that starts with `http` or `git` - path to repository to push.

## Examples

Rendered via [GitExtensions](https://github.com/gitextensions/gitextensions).

### [Kochurkins](https://github.com/KvanTTT/Kochurkins.git)

My tree. Generation script available on [gist](https://gist.github.com/KvanTTT/4a713955a54a062313d43ebb5a96824a).

![Kochurkins](https://habrastorage.org/webt/yh/yy/rd/yhyyrddjg12ufgdhyvfvis06au8.png)

### [Presidents](https://github.com/KvanTTT/Presidents.git)

USA presidents until 1988 year (2145 persons). Source: [webtreeprint.com](https://webtreeprint.com/tp_famous_gedcoms.php).

![Presidents](https://habrastorage.org/webt/m4/jo/3b/m4jo3bciwk6p18vffeuti6hqr6a.png)