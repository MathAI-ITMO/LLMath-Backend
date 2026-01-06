# Controller Logic Analysis

This document identifies business logic that exists in controllers and should potentially be moved to service layers.

## Summary

Controllers should be thin and primarily handle:
- HTTP request/response mapping
- Authorization checks
- Delegating to services
- Basic validation

Business logic, data access, and complex orchestration should be in services.

---

## Controllers with Significant Business Logic

### 1. **GeolinProxyController** ⚠️ **HIGH PRIORITY**

**Location:** `src/MathLLMBackend.Presentation/Controllers/GeolinProxyController.cs`

**Issues Found:**

#### `GetProblemDataByPrefix` method (lines 35-188):
- **Problem matching logic** (lines 58-76): Complex logic for finding exact matches vs. first result
- **Seed generation logic** (lines 87-101): Random seed generation (1 to 1,000,000,000)
- **Seed extraction from JSON** (lines 126-156): Parsing `ProblemParams` JSON to extract seed
- **Error message formatting**: Custom error response construction

**Recommendation:** Move to `GeolinService`:
- `GetProblemDataByPrefixAsync(string prefix, int? seed)`
- `FindProblemByPrefixAsync(string prefix)`
- `ExtractSeedFromProblemParams(string problemParams, int? fallbackSeed)`

#### `CheckAnswerDirect` method (lines 194-341):
- **HTTP client creation** (line 234): Creating HttpClient directly in controller
- **Payload construction logic** (lines 237-264): Complex JSON payload building with conditional logic
- **Direct API call** (lines 266-276): Making HTTP calls directly
- **Response parsing** (lines 292-312): Parsing JSON response and evaluating verdict
- **Verdict evaluation logic** (line 309): `verdict >= 1.0` business rule

**Recommendation:** Move to `GeolinService`:
- `CheckAnswerDirectAsync(CheckAnswerRequest request)`
- The entire HTTP client logic should be in the service layer

---

### 2. **AuthController** ⚠️ **MEDIUM PRIORITY**

**Location:** `src/MathLLMBackend.Presentation/Controllers/AuthController.cs`

**Issues Found:**

#### `Register` method (lines 27-108):
- **User existence check** (lines 35-49): Business logic for checking if user exists
- **Error message translation/mapping** (lines 73-99): Complex logic for translating Identity errors to Russian messages
- **Error response formatting** (lines 39-48, 101-107): Custom error response construction

**Recommendation:** Create `IAuthService`:
- `RegisterAsync(RegisterDto dto)` - handles all registration logic
- `TranslateIdentityErrors(IdentityResult result)` - error translation logic
- Controller should only call service and map responses

---

### 3. **UserTasksController** ⚠️ **MEDIUM PRIORITY**

**Location:** `src/MathLLMBackend.Presentation/Controllers/UserTasksController.cs`

**Issues Found:**

#### `StartUserTask` method (lines 70-152):
- **Complex orchestration** (lines 81-146): Multiple service calls with conditional logic
- **Workflow coordination**: Getting task → creating chat → updating task status
- **Validation logic** (lines 90-103): Checking if chat already exists
- **Error handling with business context** (lines 111-123, 128-135, 138-143)

**Recommendation:** Move orchestration to `IUserTaskService`:
- `StartUserTaskWithChatAsync(Guid userTaskId, string userId)` - handles entire workflow
- Controller should only extract userId and call service

---

### 4. **StatsController** ⚠️ **MEDIUM PRIORITY**

**Location:** `src/MathLLMBackend.Presentation/Controllers/StatsController.cs`

**Issues Found:**

#### `GetUserStats` method (lines 35-53):
- **Direct database queries** (lines 38-50): EF Core queries in controller
- **Complex LINQ aggregations**: Counting tasks by status, counting chats
- **DTO construction**: Building DTOs directly from queries

#### `GetUserDetails` method (lines 55-90):
- **Multiple database queries** (lines 58-82): Three separate queries
- **DTO construction**: Building complex DTOs in controller

**Recommendation:** Create `IStatsService`:
- `GetUserStatsAsync()` - returns all user statistics
- `GetUserDetailsAsync(string userId)` - returns user details
- Move all EF Core queries to service layer

---

## Controllers with Minor Logic Issues

### 5. **ChatController** ⚠️ **LOW PRIORITY**

**Location:** `src/MathLLMBackend.Presentation/Controllers/ChatController.cs`

**Issues Found:**

- **Authorization checks** (lines 30-34, 51-55, 80-84): Repeated userId extraction pattern
- **Ownership validation** (lines 93-96): `chat.UserId != userId` check
- **DTO mapping** (lines 41-42, 58, 73): DTO construction in controller

**Recommendation:** 
- Consider moving ownership validation to service layer
- DTO mapping is acceptable but could use AutoMapper or dedicated mappers

---

### 6. **MessagesController** ⚠️ **LOW PRIORITY**

**Location:** `src/MathLLMBackend.Presentation/Controllers/MessagesController.cs`

**Issues Found:**

- **Authorization checks** (lines 30-34, 70-74): Repeated userId extraction
- **Ownership validation** (lines 42-45, 82-85): `chat.UserId != userId` checks
- **DTO filtering** (line 90): `.Where(m => !m.IsSystemPrompt)` - business rule in controller
- **DTO mapping** (line 91): DTO construction

**Recommendation:**
- Move ownership validation to service layer
- Move system prompt filtering to service layer (or return filtered results from service)

---

## Controllers with Minimal Logic ✅

### 7. **LlmController**
- Properly delegates to `ILlmService`
- Only has basic validation and error handling
- **Status:** ✅ Good

### 8. **TasksController**
- Properly delegates to `IGeolinService`
- Only has DTO mapping
- **Status:** ✅ Good

### 9. **UserController**
- Properly delegates to `UserManager`
- Only has basic authorization check
- **Status:** ✅ Good

---

## Recommendations Summary

### High Priority Refactoring:
1. **GeolinProxyController**: Extract all business logic to `GeolinService`
   - Problem lookup and matching
   - Seed generation/extraction
   - HTTP client operations
   - Answer checking logic

### Medium Priority Refactoring:
2. **AuthController**: Create `IAuthService` for registration logic
3. **UserTasksController**: Move orchestration logic to `IUserTaskService`
4. **StatsController**: Create `IStatsService` for all statistics queries

### Low Priority Improvements:
5. **ChatController** & **MessagesController**: Move ownership validation to services
6. Consider using AutoMapper or dedicated mapper classes for DTO construction

---

## Patterns to Extract

### Common Patterns Found:
1. **UserId Extraction**: Repeated `_userManager.GetUserId(User)` pattern - could be middleware or base controller method
2. **Ownership Validation**: `chat.UserId != userId` checks - should be in service layer
3. **Error Response Construction**: Custom error DTOs - could use standardized error handling middleware
4. **DTO Mapping**: Manual DTO construction - consider AutoMapper or dedicated mappers

---

## Notes

- Controllers should be thin and focus on HTTP concerns
- Business logic belongs in services
- Data access should be in repositories/services, not controllers
- Complex orchestration should be in services, not controllers