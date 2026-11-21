#!/usr/bin/env perl
# F167, run once. Reads file paths from git grep -z on its input and rewrites each old project name in them by the
# rule in Names.pm. "dry" reads and counts and writes nothing; "write" saves each file it changed, byte for byte
# otherwise, line endings kept. Either way it prints a report: the files and names replaced, then every
# louis-agent.* word still there after the rewrite, for a person to read (Louis's pick A of finding 4).

use strict;
use warnings;
use FindBin qw($Bin);
use lib $Bin;
use Names qw(read_names);

my ($names_file, $mode) = @ARGV;
die "say dry or write\n" unless defined $mode && $mode =~ /\A(?:dry|write)\z/;
my ($new, $whole) = read_names($names_file);

my (%per_name, %leftover, $files, $replaced);
local $/ = "\0";
while (my $path = <STDIN>) {
    chomp $path;
    open my $in, '<:raw', $path or die "can't read $path: $!";
    my $text = do { local $/; <$in> };
    close $in;

    # Each whole old name replaced, counted by name.
    my $count = $text =~ s/$whole/$per_name{$1}++; $new->{$1}/ge;

    # Every louis-agent.* word left after the rewrite, with its line.
    while ($text =~ /(?<![A-Za-z0-9._-])(louis-agent\.[a-z0-9.-]*[a-z0-9])/g) {
        my $line = 1 + (substr($text, 0, $-[1]) =~ tr/\n//);
        push @{ $leftover{$1} }, "$path:$line";
    }

    next unless $count;
    $files++;
    $replaced += $count;
    next unless $mode eq 'write';
    open my $out, '>:raw', $path or die "can't write $path: $!";
    print {$out} $text;
    close $out or die "can't write $path: $!";
}

print "F167 rename ($mode): ", $replaced // 0, " names replaced in ", $files // 0, " files.\n\n";
print "Each old name, and how many places it was replaced in:\n";
printf "%6d  %s -> %s\n", $per_name{$_}, $_, $new->{$_} for sort keys %per_name;
my @never = grep { !$per_name{$_} } sort keys %$new;
print "\nOld names found nowhere in the text: @never\n" if @never;

print "\nlouis-agent.* words left for a person, each with how many places and the first three:\n";
for my $word (sort { @{ $leftover{$b} } <=> @{ $leftover{$a} } || $a cmp $b } keys %leftover) {
    my @at = @{ $leftover{$word} };
    printf "%6d  %s  %s\n", scalar @at, $word, join(' ', @at[0 .. ($#at < 2 ? $#at : 2)]);
}
