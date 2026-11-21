#!/usr/bin/env perl
# F167, run once. Prints moves.tsv: every tracked folder and file whose name begins with an old project name, and
# where it goes, one "from<TAB>to" a line, the deepest first, so a project file is renamed inside its folder before
# the folder itself moves and every path git is given still exists. Only the last part of each path changes: the
# folders above it move on their own line.

use strict;
use warnings;
use FindBin qw($Bin);
use lib $Bin;
use Names qw(read_names rename_in);

my ($names_file) = @ARGV;
my ($new, $whole) = read_names($names_file);

# Every tracked file, and every folder that holds one.
open my $git, '-|', 'git', '-c', 'core.quotepath=off', 'ls-files' or die "can't run git: $!";
my %paths;
while (my $file = <$git>) {
    chomp $file;
    my @parts = split m{/}, $file;
    $paths{ join '/', @parts[0 .. $_] } = 1 for 0 .. $#parts;
}
close $git or die "git ls-files failed\n";

my @moves;
for my $path (keys %paths) {
    my ($above, $name) = $path =~ m{^(?:(.*)/)?([^/]+)\z};
    my $renamed = $name;
    next unless rename_in(\$renamed, $new, $whole);
    push @moves, [ $path, defined $above ? "$above/$renamed" : $renamed ];
}

print "$_->[0]\t$_->[1]\n"
    for sort { ($b->[0] =~ tr{/}{}) <=> ($a->[0] =~ tr{/}{}) || $a->[0] cmp $b->[0] } @moves;
