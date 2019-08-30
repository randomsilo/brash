using System;
using System.Collections;
using System.Collections.Generic;

namespace brashcli.Model
{
    
    public class Field
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public class Reference
    {
        public string ColumnName { get; set; }
        public string TableName { get; set; }
    }

    public class Structure
    {
        public string Name { get; set; }
        public string IdPattern { get; set; }
        public string TrackingPattern { get; set; }
        public List<string> AdditionalPatterns { get; set; }
        public List<string> Choices { get; set; }
        public List<string> AdditionalSqlStatements { get; set; }
        public List<Field> Fields { get; set; }
        public List<Reference> References { get; set; }
        public List<Structure> Extensions { get; set; }
        public List<Structure> Children { get; set; }
    }

    public class DomainStructure
    {
        public string Domain { get; set; }
        public List<Structure> Structure { get; set; }
    }
}