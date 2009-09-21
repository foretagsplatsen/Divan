
EXTRA_DIST =  Makefile

# Warning: This is an automatically generated file, do not edit!

srcdir=.
top_srcdir=../..

include $(top_srcdir)/config.make

ifeq ($(CONFIG),DEBUG)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG;TRACE" "-main:Trivial.Program"
ASSEMBLY = bin/Debug/Trivial.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../../src/bin/Debug/Divan.dll
BUILD_DIR = bin/Debug/

NEWTONSOFT_JSON_DLL_SOURCE=../../../../../Divan/bin/Debug/Newtonsoft.Json.dll
NEWTONSOFT_JSON_DLL_MDB_SOURCE=../../../../../Divan/bin/Debug/Newtonsoft.Json.dll.mdb
DIVAN_DLL_MDB_SOURCE=../../src/bin/Debug/Divan.dll.mdb
DIVAN_DLL_MDB=$(BUILD_DIR)/Divan.dll.mdb
DIVAN_DLL_SOURCE=../../src/bin/Debug/Divan.dll
TRIVIAL_EXE_MDB_SOURCE=bin/Debug/Trivial.exe.mdb
TRIVIAL_EXE_MDB=$(BUILD_DIR)/Trivial.exe.mdb

endif

ifeq ($(CONFIG),RELEASE)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-define:TRACE" "-main:Trivial.Program"
ASSEMBLY = bin/Release/Trivial.exe
ASSEMBLY_MDB = 
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../../src/bin/Release/Divan.dll
BUILD_DIR = bin/Release/

NEWTONSOFT_JSON_DLL_SOURCE=../../../../../Divan/bin/Debug/Newtonsoft.Json.dll
NEWTONSOFT_JSON_DLL_MDB_SOURCE=../../../../../Divan/bin/Debug/Newtonsoft.Json.dll.mdb
DIVAN_DLL_MDB=
DIVAN_DLL_SOURCE=../../src/bin/Release/Divan.dll
TRIVIAL_EXE_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(NEWTONSOFT_JSON_DLL) \
	$(NEWTONSOFT_JSON_DLL_MDB) \
	$(DIVAN_DLL_MDB) \
	$(DIVAN_DLL) \
	$(TRIVIAL_EXE_MDB)  

BINARIES = \
	$(TRIVIAL)  


RESGEN=resgen2

TRIVIAL = $(BUILD_DIR)/trivial
NEWTONSOFT_JSON_DLL = $(BUILD_DIR)/Newtonsoft.Json.dll
NEWTONSOFT_JSON_DLL_MDB = $(BUILD_DIR)/Newtonsoft.Json.dll.mdb
DIVAN_DLL = $(BUILD_DIR)/Divan.dll

FILES = \
	Program.cs \
	Properties/AssemblyInfo.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	trivial.in 

REFERENCES =  \
	System \
	System.Core \
	System.Xml.Linq \
	System.Data.DataSetExtensions \
	System.Data \
	System.Xml

DLL_REFERENCES =  \
	../../../../../Divan/bin/Debug/Newtonsoft.Json.dll

CLEANFILES = $(PROGRAMFILES) $(BINARIES) 

#Targets
all: $(ASSEMBLY) $(PROGRAMFILES) $(BINARIES)  $(top_srcdir)/config.make

include $(top_srcdir)/Makefile.include
#include $(srcdir)/custom-hooks.make



$(eval $(call emit-deploy-wrapper,TRIVIAL,trivial,x))
$(eval $(call emit-deploy-target,NEWTONSOFT_JSON_DLL))
$(eval $(call emit-deploy-target,NEWTONSOFT_JSON_DLL_MDB))
$(eval $(call emit-deploy-target,DIVAN_DLL_MDB))
$(eval $(call emit-deploy-target,DIVAN_DLL))


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
	mkdir -p '$(DESTDIR)$(bindir)'
	$(call cp,$(TRIVIAL),$(DESTDIR)$(bindir))
	$(call cp,$(NEWTONSOFT_JSON_DLL),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call cp,$(NEWTONSOFT_JSON_DLL_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call cp,$(DIVAN_DLL_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call cp,$(DIVAN_DLL),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call cp,$(TRIVIAL_EXE_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	make post-install-local-hook prefix=$(prefix)

uninstall-local: $(ASSEMBLY) $(ASSEMBLY_MDB)
	make pre-uninstall-local-hook prefix=$(prefix)
	make uninstall-satellite-assemblies prefix=$(prefix)
	$(call rm,$(ASSEMBLY),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(ASSEMBLY_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(TRIVIAL),$(DESTDIR)$(bindir))
	$(call rm,$(NEWTONSOFT_JSON_DLL),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(NEWTONSOFT_JSON_DLL_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(DIVAN_DLL_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(DIVAN_DLL),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(TRIVIAL_EXE_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	make post-uninstall-local-hook prefix=$(prefix)

install: install-local
uninstall: uninstall-local
clean: clean-local

include $(top_srcdir)/rules.make
