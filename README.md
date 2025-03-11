# Lightweight-Serialization
Lightweight-Serialization provides tools for quickly serializing game classes to save some gamestate.\
Implement the ``ISerializable`` interface to mark a class as serializable.\
If you want to you can then either manually overwrite the ``Serialize`` and ``Deserialize`` methods or rely on the default implementation using reflections.\
As of now only a YAML backend is implemented but a binary serialization should be trivial to ammend.
