namespace twitterXcrypto.twitter
{
    public struct User : IEquatable<User>
    {
        internal string Name { get; init; }
        internal long Id { get; init; }

        #region overrides
        public override string ToString() => Name;

        public override bool Equals(object? obj)
        {
            return obj is User user && Equals(user);
        }

        public bool Equals(User other)
        {
            return Name == other.Name &&
                   Id == other.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Id);
        }

        public static bool operator ==(User left, User right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(User left, User right)
        {
            return !(left == right);
        }
        #endregion
    }
}
