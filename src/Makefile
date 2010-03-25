
EXTRA_DIST =  Makefile

# Warning: This is an automatically generated file, do not edit!

srcdir=.
top_srcdir=..

include $(top_srcdir)/config.make

ifeq ($(CONFIG),DEBUG)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:3 -optimize- -debug "-define:DEBUG;TRACE"
ASSEMBLY = bin/Debug/Divan.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Debug/

NEWTONSOFT_JSON_DLL_SOURCE=../lib/Newtonsoft.Json.dll
NUNIT_FRAMEWORK_DLL_SOURCE=../lib/nunit.framework.dll
DIVAN_DLL_MDB_SOURCE=bin/Debug/Divan.dll.mdb
DIVAN_DLL_MDB=$(BUILD_DIR)/Divan.dll.mdb

endif

ifeq ($(CONFIG),RELEASE)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:3 -optimize+ "-define:TRACE"
ASSEMBLY = bin/Release/Divan.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Release/

NEWTONSOFT_JSON_DLL_SOURCE=../lib/Newtonsoft.Json.dll
NUNIT_FRAMEWORK_DLL_SOURCE=../lib/nunit.framework.dll
DIVAN_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(NEWTONSOFT_JSON_DLL) \
	$(NUNIT_FRAMEWORK_DLL) \
	$(DIVAN_DLL_MDB)  

LINUX_PKGCONFIG = \
	$(DIVAN_PC)  


RESGEN=resgen2

NEWTONSOFT_JSON_DLL = $(BUILD_DIR)/Newtonsoft.Json.dll
NUNIT_FRAMEWORK_DLL = $(BUILD_DIR)/nunit.framework.dll
DIVAN_PC = $(BUILD_DIR)/divan.pc

FILES =  \
	CouchBulkDeleteDocuments.cs \
	CouchBulkDocuments.cs \
	CouchBulkKeys.cs \
	CouchConflictException.cs \
	CouchDatabase.cs \
	CouchDesignDocument.cs \
	CouchDocument.cs \
	CouchDocumentWrapper.cs \
	CouchException.cs \
	CouchGenericViewResult.cs \
	CouchJsonDocument.cs \
	CouchNotFoundException.cs \
	CouchPermanentViewResult.cs \
	CouchQuery.cs \
	CouchQueryDocument.cs \
	CouchRecord.cs \
	CouchRequest.cs \
	CouchServer.cs \
	CouchViewDefinition.cs \
	CouchViewDefinitionBase.cs \
	CouchViewResult.cs \
	CouchViewResultStream.cs \
	ICanJson.cs \
	ICouchDatabase.cs \
	ICouchDocument.cs \
	ICouchRequest.cs \
	ICouchServer.cs \
	ICouchViewDefinition.cs \
	ICouchViewDefinitionBase.cs \
	IReconcilingDocument.cs \
	ISelfContained.cs \
	Linq/CouchLinqQuery.cs \
	Linq/CouchQueryProvider.cs \
	Linq/ExpressionVisitor.cs \
	Linq/TypeSystem.cs \
	Lucene/CouchLuceneHit.cs \
	Lucene/CouchLuceneQuery.cs \
	Lucene/CouchLuceneViewDefinition.cs \
	Lucene/CouchLuceneViewResult.cs \
	Properties/AssemblyInfo.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	divan.pc.in 

REFERENCES =  \
	../lib/Newtonsoft.Json.dll \
	../lib/nunit.framework.dll \
	System \
	System.Core \
	System.Web

DLL_REFERENCES =  \
	../lib/Newtonsoft.Json.dll \
	../lib/nunit.framework.dll

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

#Targets
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG)  $(top_srcdir)/config.make

include $(top_srcdir)/Makefile.include
#include $(srcdir)/custom-hooks.make



$(eval $(call emit-deploy-target,NEWTONSOFT_JSON_DLL))
$(eval $(call emit-deploy-target,NUNIT_FRAMEWORK_DLL))
$(eval $(call emit-deploy-wrapper,DIVAN_PC,divan.pc))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'


$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	make pre-all-local-hook prefix=$(prefix)
	mkdir -p $(shell dirname $(ASSEMBLY))
	make $(CONFIG)_BeforeBuild
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
	make $(CONFIG)_AfterBuild
	make post-all-local-hook prefix=$(prefix)

install-local: $(ASSEMBLY) $(ASSEMBLY_MDB)
	make pre-install-local-hook prefix=$(prefix)
	make install-satellite-assemblies prefix=$(prefix)
	mkdir -p '$(DESTDIR)$(libdir)/$(PACKAGE)'
	$(call cp,$(ASSEMBLY),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call cp,$(ASSEMBLY_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call cp,$(NEWTONSOFT_JSON_DLL),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call cp,$(NUNIT_FRAMEWORK_DLL),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call cp,$(DIVAN_DLL_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	mkdir -p '$(DESTDIR)$(libdir)/pkgconfig'
	$(call cp,$(DIVAN_PC),$(DESTDIR)$(libdir)/pkgconfig)
	make post-install-local-hook prefix=$(prefix)

uninstall-local: $(ASSEMBLY) $(ASSEMBLY_MDB)
	make pre-uninstall-local-hook prefix=$(prefix)
	make uninstall-satellite-assemblies prefix=$(prefix)
	$(call rm,$(ASSEMBLY),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(ASSEMBLY_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(NEWTONSOFT_JSON_DLL),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(NUNIT_FRAMEWORK_DLL),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(DIVAN_DLL_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(DIVAN_PC),$(DESTDIR)$(libdir)/pkgconfig)
	make post-uninstall-local-hook prefix=$(prefix)

install: install-local
uninstall: uninstall-local
clean: clean-local

include $(top_srcdir)/rules.make
