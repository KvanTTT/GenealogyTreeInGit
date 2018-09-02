﻿using System;
using System.Collections.Generic;

namespace GenealogyTreeInGit.Git
{
    public class Family
    {
        public string Title { get; }

        public Dictionary<string, GitPerson> Persons { get; }

        public List<GitPersonEvent> Events { get; }

        public Family(string title, Dictionary<string, GitPerson> persons)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Persons = persons ?? throw new ArgumentNullException(nameof(persons));
        }
    }
}
