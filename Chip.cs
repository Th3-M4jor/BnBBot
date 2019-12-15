namespace csharp
{
    public class Chip : IBnBObject
    {
        public string Name { get; private set; }
        public string[] Element { get; private set; }

        public string[] Skill { get; private set; }

        public string Range { get; private set; }

        public string Damage { get; private set; }

        public string Hits { get; private set; }

        public string Type { get; private set; }
        public string Description { get; private set; }
        public string All { get; private set; }

        public string SkillTarget { get; private set; }

        public string SkillUser { get; private set; }

        public Chip(string name, string range, string damage, string hits, string type, string[] element, string[] skill, string description, string all, string skillUser, string skillTarget)
        {
            this.Name = name ?? throw new System.ArgumentNullException();
            this.Range = range ?? throw new System.ArgumentNullException();
            this.Damage = damage ?? throw new System.ArgumentNullException();
            this.Hits = hits ?? throw new System.ArgumentNullException();
            this.Type = type ?? throw new System.ArgumentNullException();
            this.Element = element ?? throw new System.ArgumentNullException();
            this.Skill = skill ?? throw new System.ArgumentNullException();
            this.Description = description ?? throw new System.ArgumentNullException();
            this.All = all ?? throw new System.ArgumentNullException();
            this.SkillUser = skillUser ?? "--";
            this.SkillTarget = skillTarget ?? "--";

        }
    }
}