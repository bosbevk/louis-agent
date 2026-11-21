#!/usr/bin/env perl
# F167, run once. Runs git mv for each line of moves.tsv, in its order, the deepest first, with git's arguments as a
# list, never through a shell. It stops at the first move git refuses, so nothing runs on past a mistake.

use strict;
use warnings;

my ($moves_file) = @ARGV;
open my $in, '<', $moves_file or die "can't read $moves_file: $!";
my $moved = 0;
while (my $line = <$in>) {
    $line =~ s/\r?\n\z//;
    my ($from, $to) = split /\t/, $line;
    system('git', 'mv', $from, $to) == 0 or die "git mv $from $to failed\n";
    $moved++;
}
print "F167 rename: $moved folders and files moved.\n";
