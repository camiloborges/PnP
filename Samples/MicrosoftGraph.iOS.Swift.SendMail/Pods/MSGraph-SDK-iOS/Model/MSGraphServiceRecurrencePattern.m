/*******************************************************************************
**NOTE** This code was generated by a tool and will occasionally be
overwritten. We welcome comments and issues regarding this code; they will be
addressed in the generation tool. If you wish to submit pull requests, please
do so for the templates in that tool.

This code was generated by Vipr (https://github.com/microsoft/vipr) using
the T4TemplateWriter (https://github.com/msopentech/vipr-t4templatewriter).

Copyright (c) Microsoft Corporation. All Rights Reserved.
Licensed under the Apache License 2.0; see LICENSE in the source repository
root for authoritative license information.﻿
******************************************************************************/



#import "MSGraphServiceModels.h"
#import "core/MSOrcObjectizer.h"


/** Implementation for MSGraphServiceRecurrencePattern
 *
 */
@implementation MSGraphServiceRecurrencePattern


@synthesize odataType = _odataType;

+ (NSDictionary *) $$$_$$$propertiesNamesMappings
{
    static NSDictionary *_$$$_$$$propertiesNamesMappings=nil; 

        if(_$$$_$$$propertiesNamesMappings==nil) {
    
        _$$$_$$$propertiesNamesMappings=[[NSDictionary alloc] initWithObjectsAndKeys:  @"type", @"type", @"interval", @"interval", @"month", @"month", @"dayOfMonth", @"dayOfMonth", @"daysOfWeek", @"daysOfWeek", @"firstDayOfWeek", @"firstDayOfWeek", @"index", @"index", nil];
        
    }
    
    return _$$$_$$$propertiesNamesMappings;
}


- (instancetype)init {

	if (self = [super init]) {

		_odataType = @"#microsoft.graph.recurrencePattern";

    }

	return self;
}


- (instancetype) initWithDictionary: (NSDictionary *) dic {
    if((self = [self init])) {
        if(dic!=nil) {
		_type = (![dic objectForKey: @"type"] || [ [dic objectForKey: @"type"] isKindOfClass:[NSNull class]] )?_type:[MSGraphServiceRecurrencePatternTypeSerializer fromString:[dic objectForKey: @"type"]];
		_interval = (![dic objectForKey: @"interval"] || [ [dic objectForKey: @"interval"] isKindOfClass:[NSNull class]] )?_interval:[[dic objectForKey: @"interval"] intValue];
		_month = (![dic objectForKey: @"month"] || [ [dic objectForKey: @"month"] isKindOfClass:[NSNull class]] )?_month:[[dic objectForKey: @"month"] intValue];
		_dayOfMonth = (![dic objectForKey: @"dayOfMonth"] || [ [dic objectForKey: @"dayOfMonth"] isKindOfClass:[NSNull class]] )?_dayOfMonth:[[dic objectForKey: @"dayOfMonth"] intValue];

        if([dic objectForKey: @"daysOfWeek"] != [NSNull null]){
            _daysOfWeek = [[MSOrcChangesTrackingArray alloc] init];
            
            for (id object in [dic objectForKey: @"daysOfWeek"]) {
                [_daysOfWeek addObject:@([MSGraphServiceDayOfWeekSerializer fromString:object])];
            }
            
            [(MSOrcChangesTrackingArray *)_daysOfWeek resetChangedFlag];
        }
        
		_firstDayOfWeek = (![dic objectForKey: @"firstDayOfWeek"] || [ [dic objectForKey: @"firstDayOfWeek"] isKindOfClass:[NSNull class]] )?_firstDayOfWeek:[MSGraphServiceDayOfWeekSerializer fromString:[dic objectForKey: @"firstDayOfWeek"]];
		_index = (![dic objectForKey: @"index"] || [ [dic objectForKey: @"index"] isKindOfClass:[NSNull class]] )?_index:[MSGraphServiceWeekIndexSerializer fromString:[dic objectForKey: @"index"]];
    }
    [self.updatedValues removeAllObjects];
    }
    
    return self;
}

- (NSDictionary *) toDictionary {
    
    NSMutableDictionary *dic=[[NSMutableDictionary alloc] init];

	{[dic setValue: [MSGraphServiceRecurrencePatternTypeSerializer toString:self.type] forKey: @"type"];}
	{[dic setValue: [NSNumber numberWithInt: self.interval] forKey: @"interval"];}
	{[dic setValue: [NSNumber numberWithInt: self.month] forKey: @"month"];}
	{[dic setValue: [NSNumber numberWithInt: self.dayOfMonth] forKey: @"dayOfMonth"];}
	{    NSMutableArray *curVal = [[NSMutableArray alloc] init];
    
    for(id obj in self.daysOfWeek) {
       [curVal addObject:[MSGraphServiceDayOfWeekSerializer toString:obj]];
    }
    
    if([curVal count]==0) curVal=nil;
if (curVal!=nil) [dic setValue: curVal forKey: @"daysOfWeek"];}
	{[dic setValue: [MSGraphServiceDayOfWeekSerializer toString:self.firstDayOfWeek] forKey: @"firstDayOfWeek"];}
	{[dic setValue: [MSGraphServiceWeekIndexSerializer toString:self.index] forKey: @"index"];}
    [dic setValue: @"#microsoft.graph.recurrencePattern" forKey: @"@odata.type"];

    return dic;
}

- (NSDictionary *) toUpdatedValuesDictionary {
    
    NSMutableDictionary *dic=[[NSMutableDictionary alloc] init];

 if([self.updatedValues containsObject:@"type"])
            { [dic setValue: [MSGraphServiceRecurrencePatternTypeSerializer toString:self.type] forKey: @"type"];
} if([self.updatedValues containsObject:@"interval"])
            { [dic setValue: [NSNumber numberWithInt: self.interval] forKey: @"interval"];
} if([self.updatedValues containsObject:@"month"])
            { [dic setValue: [NSNumber numberWithInt: self.month] forKey: @"month"];
} if([self.updatedValues containsObject:@"dayOfMonth"])
            { [dic setValue: [NSNumber numberWithInt: self.dayOfMonth] forKey: @"dayOfMonth"];
} if([self.updatedValues containsObject:@"daysOfWeek"])
            { [dic setValue: [MSGraphServiceDayOfWeekSerializer toString:self.daysOfWeek] forKey: @"daysOfWeek"];
} if([self.updatedValues containsObject:@"firstDayOfWeek"])
            { [dic setValue: [MSGraphServiceDayOfWeekSerializer toString:self.firstDayOfWeek] forKey: @"firstDayOfWeek"];
} if([self.updatedValues containsObject:@"index"])
            { [dic setValue: [MSGraphServiceWeekIndexSerializer toString:self.index] forKey: @"index"];
}    return dic;
}


/** Setter implementation for property type
 *
 */
- (void) setType: (MSGraphServiceRecurrencePatternType) value {
    _type = value;
    [self valueChangedFor:@"type"];
}
       

- (void)setTypeString:(NSString *)string {
        
    _type = [MSGraphServiceRecurrencePatternTypeSerializer fromString:string];
    [self valueChangedFor:@"type"]; 
}

/** Setter implementation for property interval
 *
 */
- (void) setInterval: (int) value {
    _interval = value;
    [self valueChangedFor:@"interval"];
}
       
/** Setter implementation for property month
 *
 */
- (void) setMonth: (int) value {
    _month = value;
    [self valueChangedFor:@"month"];
}
       
/** Setter implementation for property dayOfMonth
 *
 */
- (void) setDayOfMonth: (int) value {
    _dayOfMonth = value;
    [self valueChangedFor:@"dayOfMonth"];
}
       
/** Setter implementation for property daysOfWeek
 *
 */
- (void) setDaysOfWeek: (NSMutableArray *) value {
    _daysOfWeek = value;
    [self valueChangedFor:@"daysOfWeek"];
}
       
/** Setter implementation for property firstDayOfWeek
 *
 */
- (void) setFirstDayOfWeek: (MSGraphServiceDayOfWeek) value {
    _firstDayOfWeek = value;
    [self valueChangedFor:@"firstDayOfWeek"];
}
       

- (void)setFirstDayOfWeekString:(NSString *)string {
        
    _firstDayOfWeek = [MSGraphServiceDayOfWeekSerializer fromString:string];
    [self valueChangedFor:@"firstDayOfWeek"]; 
}

/** Setter implementation for property index
 *
 */
- (void) setIndex: (MSGraphServiceWeekIndex) value {
    _index = value;
    [self valueChangedFor:@"index"];
}
       

- (void)setIndexString:(NSString *)string {
        
    _index = [MSGraphServiceWeekIndexSerializer fromString:string];
    [self valueChangedFor:@"index"]; 
}


@end
