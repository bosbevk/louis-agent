package Names;

# F167, run once (Louis, 2026-11-21: "3 just as a once off just too many to test"). The rule every other file here
# uses: which old project name becomes which new one, and where a name counts as a whole name.

use strict;
use warnings;
use Exporter 'import';
our @EXPORT_OK = qw(read_names rename_in);

# Reads names.tsv, one "old<TAB>new" a line, and gives back the map and the pattern of a whole old name.
# A name counts only where no letter, digit, dot, dash or underscore stands before it, and after it comes the end,
# a character that ends the word, a picture's dash, a sentence's full stop, or one of the endings a file named for
# the project carries. The longest names are tried first, so louis-agent.core.web is never half-changed by
# louis-agent.core, and louis-agent.dev, which is no project, never matches.
sub read_names {
    my ($file) = @_;
    open my $in, '<', $file or die "can't read $file: $!";
    my %new;
    while (my $line = <$in>) {
        $line =~ s/\r?\n\z//;
        next if $line eq '' || $line =~ /^#/;
        my ($old, $renamed) = split /\t/, $line;
        $new{$old} = $renamed;
    }
    my $names = join '|', map { quotemeta } sort { length($b) <=> length($a) || $a cmp $b } keys %new;
    my $endings = qr/\.(?:csproj|md|svg|dll|json|styles\.css)(?![A-Za-z0-9._-])/;
    # A name may also start right after a JSON label's \n escape, whose n would otherwise read as part of a word.
    my $whole = qr/(?:(?<![A-Za-z0-9._-])|(?<=\\n))($names)(?=\z|[^A-Za-z0-9._-]|-|$endings|\.(?![A-Za-z0-9]))/;
    return (\%new, $whole);
}

# Rewrites every whole old name in the text it is given, in place, and gives back how many it replaced.
sub rename_in {
    my ($text, $new, $whole) = @_;
    return $$text =~ s/$whole/$new->{$1}/g;
}

1;
