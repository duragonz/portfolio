# 포트 폴리오



### 임화용 : 게임 서버 개발자

| Birthday  | 1974. 04. 23                                       |
| --------- | -------------------------------------------------- |
| Email     | [duragonz@naver.com](mailto:duragonz@naver.com)    |
| Mobile    | 010-6825-0077                                      |
| Address   | 경기도 성남시 분당구 분당로 201번길 17 105동 301호 |
| portfolio | https://github.com/duragonz/portfolio              |



## 샘플코드 설명

#### AspNetCore

* AspNetCore 구동부 및 Controller  샘플

#### DBConnector

![image](https://github.com/duragonz/portfolio/assets/57695329/7b2426e5-2843-41d7-af1e-4155a34475ad)

* DbDemultiplexer : 로직에서 DB 접근을 위한 dbBroker thread 생성 및 할당
  * socket bind시 할당/ unbind시 해제
* DbBroker
  * 사용자 그룹별 DbBroker를 Thread로 할당하여 DB 처리
* sql_datalayer
  * provider : MsSQL, MySQL 연결 provider
  * reader : DB 결과를 읽어서 형식에 맞는 타입 변환
  * request : DB Query request

#### DBQuery

* StoredProcedure 샘플 
* _generated : QueryBuilder에서 생성된 csharp 생성 코드 샘플
  * QueryBuilder : DB 처리에 사용되는 SP주석에 특별한 주석을 삽입하고 이를 이용해서 SP 처리 결과를 자동으로 생성하는 툴

#### csharpSocket

* socket, TcpConnection 및 MessageHandler 샘플

#### invetory

* Item 구조 및 Inventory
* DB 저장을 위한 ItemDbUpdater 샘플


## 개발툴
* [GenerationTool](https://github.com/duragonz/portfolio/blob/ba7dbe9ad7f3f35132748b9a0db2704d4feb562b/Tool_DB_%EC%BD%94%EB%93%9C%EC%83%9D%EC%84%B1_%EC%9E%90%EB%8F%99%ED%99%94.md) : 서버 개발에 필요한 기능중에, 패턴이 있는 코드를 자동 생성하여 개발 효율 증대
* [ExcelDiff](https://github.com/duragonz/portfolio/blob/ba7dbe9ad7f3f35132748b9a0db2704d4feb562b/Tool_ExcelDiff.md) : 기획에서 사용하는 Excel 데이터를 편하게 비교 및 병합할 수 있는 Tool
* [StressTestModel](https://github.com/duragonz/portfolio/blob/ba7dbe9ad7f3f35132748b9a0db2704d4feb562b/Tool_StressTestModel.md) : 서버 개발에 필수적인 StressTest를 위한 구성
