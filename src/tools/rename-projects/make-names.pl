#!/usr/bin/env perl
# F167, run once. Prints names.tsv: every tracked louis-agent.* project's name today and the name it takes, its
# RootNamespace (Louis's pick A of finding 3), or the name listed below where the namespace can't say it (finding 6).
# It stops, naming them, if two projects would end up with one name, or one sets no RootNamespace.

use strict;
use warnings;
use File::Basename qw(basename);

# The eight whose namespace is shared with a neighbour that keeps it, or says less than their name, and the coding
# catalog's tests, which follow their project (Louis, 2026-11-21, "nxt" to that recommendation in step 2).
my %listed = (
    'louis-agent.evals.checks'                   => 'LouisAgent.Evals.Checks',
    'louis-agent.identity.puls.evals'            => 'LouisAgent.Identities.Puls.Evals',
    'louis-agent.identity.puls.evals.tests'      => 'LouisAgent.Identities.Puls.Evals.Tests',
    'louis-agent.pack.recipes.checks'            => 'LouisAgent.Packs.Recipes.Checks',
    'louis-agent.pack.recipes.checks.tests'      => 'LouisAgent.Packs.Recipes.Checks.Tests',
    'louis-agent.pack.where-to-buy.checks'       => 'LouisAgent.Packs.WhereToBuy.Checks',
    'louis-agent.pack.where-to-buy.checks.tests' => 'LouisAgent.Packs.WhereToBuy.Checks.Tests',
    'louis-agent.coding-catalog'                 => 'LouisAgent.Coding.Catalog',
    'louis-agent.coding-catalog.tests'           => 'LouisAgent.Coding.Catalog.Tests',
);

# Git's list of tracked project files, asked with its arguments as a list, never through a shell.
open my $git, '-|', 'git', 'ls-files', '*.csproj' or die "can't run git: $!";
my %new;
while (my $file = <$git>) {
    chomp $file;
    my $old = basename($file, '.csproj');
    next unless $old =~ /^louis-agent\./;
    if (exists $listed{$old}) { $new{$old} = $listed{$old}; next; }

    open my $project, '<', $file or die "can't read $file: $!";
    my ($namespace) = do { local $/; <$project> } =~ m{<RootNamespace>\s*([^<]+?)\s*</RootNamespace>};
    die "$old sets no RootNamespace, so its new name can't be read: set one, or list its new name\n"
        unless defined $namespace;
    $new{$old} = $namespace;
}
close $git or die "git ls-files failed\n";

# Two projects with one new name would land in one folder: stop before anything is touched.
my %by_new;
push @{ $by_new{ $new{$_} } }, $_ for keys %new;
for my $name (sort keys %by_new) {
    my @projects = sort @{ $by_new{$name} };
    die "$name would be the name of " . join(', ', @projects) . ": list a new name for all but one of them\n"
        if @projects > 1;
}

print "# F167: each louis-agent.* project's name today, and the name it takes.\n";
print "$_\t$new{$_}\n" for sort keys %new;
