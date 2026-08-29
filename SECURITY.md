# Security Policy

## 1. Threat Mitigation Standards

- **JWT Token Management**: HS256 / RS256 token signatures with strict expiration timeouts.
- **Password Protection**: Passwords hashed using salted `bcrypt` algorithms. Plaintext passwords never persisted.
- **SQL Injection Prevention**: All database interactions use SQLAlchemy 2.0 parameterized queries and async ORM mapping.
- **Rate Limiting**: Redis-backed token buckets mitigate denial of service and brute-force attacks on authentication endpoints.

## 2. Reporting Vulnerabilities

If you discover a security vulnerability within Shadow Protocol, please email `security@shadowprotocol.internal` or open a private security advisory on GitHub.
